/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
*
*/
/*
*   Copyright (C) 2022-2023 by Bryan Biedenkapp N2PLL, Patrick McDonnell W3AXL
*
*   This program is free software: you can redistribute it and/or modify
*   it under the terms of the GNU Affero General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU Affero General Public License for more details.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using Serilog;

using fnecore;
using fnecore.P25;
using System.Reflection;
using System.Data.SqlTypes;

namespace dvmdfsi.DFSI
{
    public class DVMModem
    {
        public const byte DVM_FRAME_START = 0xFE;
        public const byte CMD_P25_DATA = 0x31;
    }

    /// <summary>
    /// Implements the DFSI RTP port interface
    /// </summary>
    public class SerialService
    {
        private bool isStarted = false;

        private SerialPort port = null;

        private bool abortListening = false;

        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;

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
        public void Send(byte[] message)
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

                Log.Logger.Debug($"({Program.Configuration.Name}) sending {buffer.Length}-byte message to serial port");
                Log.Logger.Verbose($"{BitConverter.ToString(buffer).Replace("-"," ")}");

                port.Write(buffer, 0, buffer.Length);
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

            abortListening = false;

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
                abortListening = true;
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

            isStarted = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private async void Listen()
        {
            CancellationToken ct = listenCancelToken.Token;
            ct.ThrowIfCancellationRequested();

            while (!abortListening && port.IsOpen)
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

                Log.Logger.Debug($"({Program.Configuration.Name}) got {length}-byte message from serial");
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
                    default:
                        Log.Logger.Warning($"({Program.Configuration.Name}) got unhandled DVM command from serial: {command}");
                        break;
                }
                
                if (ct.IsCancellationRequested)
                    abortListening = true;
            }
        }
    } // public class RTPService
} // namespace dvmdfsi.DFSI
