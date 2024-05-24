// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Patrick McDonnell, W3AXL
*
*/
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;

using Serilog;

using fnecore;
using fnecore.P25;
using System.Reflection;
using System.Data.SqlTypes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using System.Buffers.Binary;

namespace dvmdfsi.DFSI
{
    public class DVMModem
    {
        public const byte DVM_FRAME_START = 0xFE;
        public const byte CMD_P25_DATA = 0x31;
        public const byte CMD_DEBUG1 = 0xF1;
        public const byte CMD_DEBUG2 = 0xF2;
        public const byte CMD_DEBUG3 = 0xF3;
        public const byte CMD_DEBUG4 = 0xF4;
        public const byte CMD_DEBUG5 = 0xF5;
    }
    
    public enum TxMsgType
    {
        Normal,
        IMBE
    }

    public class SerialMessage
    {
        // Byte message
        public byte[] Data {get; set;}
        // Timestamp to send the data over the port
        public long Timestamp {get; set;}
        
    }

    /// <summary>
    /// Implements the DFSI RTP port interface
    /// </summary>
    public class SerialService
    {
        private bool isStarted = false;

        private SerialPort port = null;

        // Thread management variables for listen task
        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;
        private bool abortListen = false;

        // Thread management variables for transmit task
        private CancellationTokenSource transmitCancelToken = new CancellationTokenSource();
        private Task transmitTask = null;
        private bool abortTransmit = false;

        // Serial TX buffer for properly metering our serial data
        private Queue<SerialMessage> TxBuffer = new Queue<SerialMessage>();
        // Stopwatch for properly timing out serial IMBE frames (poor man's jitter buffer)
        private Stopwatch txMessageTimer = new Stopwatch();
        private SerialMessage lastMessage;

        /*
        ** Properties
        */

        /// <summary>
        /// Flag indicating whether this <see cref="RTP"/> is running.
        /// </summary>
        public bool IsStarted => isStarted;

        /*
        ** Events/Callbacks
        */

        /// <summary>
        /// Event action that handles a raw network frame directly.
        /// </summary>
        public event RTPNetworkFrame RTPFrameHandler;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="RTP"/> class.
        /// </summary>
        static SerialService()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RTP"/> class.
        /// </summary>
        /// <param name="endpoint"></param>
        public SerialService(string portname, int baudrate)
        {
            Log.Logger.Debug($"Loading System.IO.Ports from {typeof(SerialPort).Assembly.Location}");
            port = new SerialPort(portname, baudrate);
        }

        /// <summary>
        /// Helper to reset the master endpoint.
        /// </summary>
        /// <param name="port"></param>
        public void ResetPort(string portname, int baudrate)
        {
            if (port != null)
            {
                if (port.IsOpen) {port.Close();}
                port.PortName = portname;
                port.BaudRate = baudrate;
            }
            else
            {
                port = new SerialPort(portname, baudrate);
            }
        }

        /// <summary>
        /// Helper to send a raw Serial frame
        /// </summary>
        /// <param name="message">byte array to send to the serial port</param>
        public void Send(byte[] message, TxMsgType type)
        {
            if (port.IsOpen)
            {
                // Format the message
                byte[] buffer = new byte[message.Length + 4];
                buffer[0] = DVMModem.DVM_FRAME_START;
                buffer[1] = (byte)(message.Length + 4);
                buffer[2] = DVMModem.CMD_P25_DATA;
                buffer[3] = 0x00;

                Array.Copy(message, 0, buffer, 4, message.Length);

                Log.Logger.Verbose($"({Program.Configuration.Name}) adding {buffer.Length}-byte message to serial TX buffer");
                Log.Logger.Verbose($"{BitConverter.ToString(buffer).Replace("-"," ")}");

                // Calculate the correct time to send this message
                long msgTime = 0;
                // if this is the first message, send timestamp is current time plus our fixed delay
                if (lastMessage == null)
                {
                    msgTime = txMessageTimer.ElapsedMilliseconds + Program.Configuration.SerialTxJitter;
                }
                // If we had a message before this, calculate the tx timestamp dynamically
                else
                {
                    long lastMsgTime = lastMessage.Timestamp;
                    // If the last message occured longer than our buffer delay, we restart our timestamp sequence
                    if (txMessageTimer.ElapsedMilliseconds - lastMsgTime > Program.Configuration.SerialTxJitter)
                    {
                        msgTime = txMessageTimer.ElapsedMilliseconds + Program.Configuration.SerialTxJitter;
                    }
                    // Otherwise, we time out messages as appropriate for the message type
                    else
                    {
                        if (type == TxMsgType.IMBE)
                        {
                            // We must make sure IMBE frames go out at exactly 20ms period
                            msgTime = lastMsgTime + 20;
                        }
                        else
                        {
                            // Any other message we don't care, so just add 5 ms (basically the minimum time a message can be)
                            msgTime = lastMsgTime + 5;
                        }
                    }
                }

                // Create the serial message object and add it to the queue
                SerialMessage serMsg = new SerialMessage
                {
                    Data = buffer,
                    Timestamp = msgTime
                };
                TxBuffer.Enqueue(serMsg);
                lastMessage = serMsg;
            }
            else
            {
                Log.Logger.Error($"({Program.Configuration.Name}) tried to send serial message to closed serial port!");
            }
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneMaster"/>.
        /// </summary>
        public void Start()
        {
            if (isStarted)
                throw new InvalidOperationException("Cannot start listening when already started.");

            Log.Logger.Information($"({Program.Configuration.Name}) starting serial port services on port {port.PortName} at {port.BaudRate} baud");

            abortListen = false;

            // Open the port
            try
            {
                port.Open();
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"({Program.Configuration.Name}) got exception while opening serial port: {ex.Message}");
            }

            // Start async listener
            listenTask = Task.Factory.StartNew(Listen, listenCancelToken.Token);

            // Start async transmitter
            transmitTask = Task.Factory.StartNew(Transmit, transmitCancelToken.Token);

            // Start tx mesage timer
            txMessageTimer.Start();

            isStarted = true;
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneMaster"/>.
        /// </summary>
        public void Stop()
        {
            if (!isStarted)
                throw new InvalidOperationException("Cannot stop listening when not started.");

            Log.Logger.Information($"({Program.Configuration.Name}) stopping serial port services on port {port.PortName} at {port.BaudRate} baud");

            // stop serial listen task
            if (listenTask != null)
            {
                abortListen = true;
                listenCancelToken.Cancel();

                try
                {
                    listenTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { /* stub */ }
                finally
                {
                    listenCancelToken.Dispose();
                }
            }

            if (transmitTask != null)
            {
                abortTransmit = true;
                transmitCancelToken.Cancel();

                try
                {
                    transmitTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { /* stub */ }
                finally
                {
                    transmitCancelToken.Dispose();
                }
            }

            // Stop the stopwatch
            txMessageTimer.Stop();
            txMessageTimer.Reset();

            isStarted = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private async void Listen()
        {
            CancellationToken ct = listenCancelToken.Token;
            ct.ThrowIfCancellationRequested();

            while (!abortListen && port.IsOpen)
            {
                // Buffer for received serial data
                var rxBuffer = new byte[64];

                // Read from the port until we see the start flag
                while (rxBuffer[0] != DVMModem.DVM_FRAME_START)
                {
                    await port.BaseStream.ReadAsync(rxBuffer, 0, 1);
                }

                // Read the length & command bytes
                await port.BaseStream.ReadAsync(rxBuffer, 1, 2);
                int length = rxBuffer[1];
                byte command = rxBuffer[2];

                // Read the data from the port based on the length given
                await port.BaseStream.ReadAsync(rxBuffer, 3, length - 3);
                var msg = new byte[length - 3];
                Array.Copy(rxBuffer, 3, msg, 0, length - 3);

                Log.Logger.Verbose($"({Program.Configuration.Name}) got {length}-byte message from serial");
                Log.Logger.Verbose($"{FneUtils.HexDump(rxBuffer, 0)}");

                // switch on command
                switch (command)
                {
                    case DVMModem.CMD_P25_DATA:
                        // extract the P25 data starting from byte 4, since there's a padded 0x00 at the start for whatever reason (original MMDVM thing)
                        var dfsiData = new byte[length - 4];
                        Array.Copy(rxBuffer, 4, dfsiData, 0, length - 4);
                        // Send to our RTPFrameHandler with a dummy UDP and RTP frame since those aren't needed here
                        RTPFrameHandler?.Invoke(new UdpFrame(), dfsiData, new RtpHeader());
                        break;
                    case DVMModem.CMD_DEBUG1:
                        // extract the debug message
                        Log.Logger.Debug($"({Program.Configuration.Name}) V24 Debug Message: {System.Text.Encoding.Default.GetString(rxBuffer, 3, length - 3)})");
                        break;
                    case DVMModem.CMD_DEBUG2:
                        // Extract param1
                        Int16 param1 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 2));
                        // extract the debug message
                        Log.Logger.Debug($"({Program.Configuration.Name}) V24 Debug Message: {System.Text.Encoding.Default.GetString(rxBuffer, 3, length - 3 - 2)} {param1})");
                        break;
                    case DVMModem.CMD_DEBUG3:
                        // Extract param1
                        param1 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 4));
                        // Extract param2
                        Int16 param2 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 2));
                        // extract the debug message
                        Log.Logger.Debug($"({Program.Configuration.Name}) V24 Debug Message: {System.Text.Encoding.Default.GetString(rxBuffer, 3, length - 3 - 4)} {param1} {param2})");
                        break;
                    case DVMModem.CMD_DEBUG4:
                        // Extract param1
                        param1 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 6));
                        // Extract param2
                        param2 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 4));
                        // Extract param3
                        Int16 param3 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 2));
                        // Print
                        Log.Logger.Debug($"({Program.Configuration.Name}) V24 Debug Message: {System.Text.Encoding.Default.GetString(rxBuffer, 3, length - 3 - 6)} {param1} {param2} {param3})");
                        break;
                    case DVMModem.CMD_DEBUG5:
                        // Extract param1
                        param1 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 8));
                        // Extract param2
                        param2 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 6));
                        // Extract param3
                        param3 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 4));
                        // Extract param4
                        Int16 param4 = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(rxBuffer, length - 2));
                        // extract the debug message
                        Log.Logger.Debug($"({Program.Configuration.Name}) V24 Debug Message: {System.Text.Encoding.Default.GetString(rxBuffer, 3, length - 3 - 8)} {param1} {param2} {param3} {param4})");
                        break;
                    default:
                        Log.Logger.Warning($"({Program.Configuration.Name}) got unhandled DVM command from serial: {command}");
                        break;
                }
                
                if (ct.IsCancellationRequested)
                    abortListen = true;
            }
        }

        private async void Transmit()
        {
            while (!abortTransmit && port.IsOpen)
            {
                // Make sure we have data to send first
                if (TxBuffer.Count > 0)
                {
                    // Get the first message in the queue, but don't remove it yet in case we have to wait
                    SerialMessage txMsg = TxBuffer.Peek();

                    // Send it if our stopwatch time has elapsed
                    if (txMessageTimer.ElapsedMilliseconds >= txMsg.Timestamp)
                    {
                        // Send
                        Log.Logger.Debug($"({Program.Configuration.Name}) wrote {txMsg.Data.Length}-byte message to serial port");
                        port.Write(txMsg.Data, 0, txMsg.Data.Length);
                        // Remove from queue
                        TxBuffer.Dequeue();
                    }
                }
                else
                {
                    // Delay
                    await Task.Delay(1);
                }
            }
        }

    } // public class RTPService
} // namespace dvmdfsi.DFSI
