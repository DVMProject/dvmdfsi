/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
*
*/
/*
*   Copyright (C) 2023 by Bryan Biedenkapp N2PLL
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
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using Serilog;

using dvmdfsi.FNE;

using dvmdfsi.DFSI.FSC;

namespace dvmdfsi.DFSI
{
    /// <summary>
    /// Callback used to signal a DFSI connection response.
    /// </summary>
    /// <param name="response"></param>
    /// <returns>True, if the frame was handled, otherwise false.</returns>
    public delegate void ControlConnectResponse(FSCConnectResponse response);

    /// <summary>
    /// Implements the DFSI control port interface.
    /// </summary>
    public class ControlService
    {
        private const int MAX_MISSED_HB = 5;
        private const int MAX_CONNECT_WAIT_CYCLES = 10;

        private static Random rand = null;

        private bool isStarted = false;

        private UdpListener server = null;
        private IPEndPoint masterEndpoint = null;

        private bool abortListening = false;

        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;
        private CancellationTokenSource maintainenceCancelToken = new CancellationTokenSource();
        private Task maintainenceTask = null;

        private int cyclesSinceConnectReq = 0;
        private bool reqConnectionToPeer = false;
        private bool establishedConnection = false;
        private DateTime lastPing;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> for this <see cref="ControlService"/>.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get
            {
                if (server != null)
                    return server.EndPoint;
                return null;
            }
        }

        /// <summary>
        /// Flag indicating whether this <see cref="ControlService"/> is running.
        /// </summary>
        public bool IsStarted => isStarted;

        /*
        ** Events/Callbacks
        */

        /// <summary>
        /// Event action that signals a DFSI connection response.
        /// </summary>
        public event ControlConnectResponse ConnectResponse;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="ControlService"/> class.
        /// </summary>
        static ControlService()
        {
            int seed = 0;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] intBytes = new byte[4];
                rng.GetBytes(intBytes);
                seed = BitConverter.ToInt32(intBytes, 0);
            }

            rand = new Random(seed);
            rand.Next();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlService"/> class.
        /// </summary>
        public ControlService() : this(new IPEndPoint(IPAddress.Any, Program.Configuration.LocalControlPort))
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlService"/> class.
        /// </summary>
        /// <param name="endpoint"></param>
        public ControlService(IPEndPoint endpoint)
        {
            server = new UdpListener(endpoint);

            // handle using address as IP or resolving from hostname to IP
            try
            {
                masterEndpoint = new IPEndPoint(IPAddress.Parse(Program.Configuration.RemoteDfsiAddress), Program.Configuration.RemoteControlPort);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Program.Configuration.RemoteDfsiAddress);
                if (addresses.Length > 0)
                    masterEndpoint = new IPEndPoint(addresses[0], Program.Configuration.RemoteControlPort);
            }
        }

        /// <summary>
        /// Helper to send a raw UDP frame.
        /// </summary>
        /// <param name="frame">UDP frame to send</param>
        public void Send(UdpFrame frame)
        {
            if (Program.Configuration.RawPacketTrace)
                Log.Logger.Debug($"({Program.Configuration.Name}) Network Sent (to {frame.Endpoint}) -- {FneUtils.HexDump(frame.Message, 0)}");

            server.Send(frame);
        }

        /// <summary>
        /// Helper to send a raw message to the DFSI remote.
        /// </summary>
        /// <param name="message">Byte array containing message to send</param>
        public void SendRemote(byte[] message)
        {
            Send(new UdpFrame()
            {
                Endpoint = masterEndpoint,
                Message = message
            });
        }

        /// <summary>
        /// Helper to send a FSC message to the DFSI remote.
        /// </summary>
        /// <param name="message">FSC message to send</param>
        public void SendRemote(FSCMessage message)
        {
            byte[] buffer = null;
            if (message.MessageId != MessageType.FSC_ACK)
                buffer = new byte[message.Length];
            else
                buffer = new byte[message.Length + ((FSCACK)message).ResponseLength];

            message.Encode(ref buffer);

            Send(new UdpFrame()
            {
                Endpoint = masterEndpoint,
                Message = buffer
            });
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneMaster"/>.
        /// </summary>
        public void Start()
        {
            if (isStarted)
                throw new InvalidOperationException("Cannot start listening when already started.");

            Log.Logger.Information($"({Program.Configuration.Name}) starting DFSI control network services, {server.EndPoint}");

            abortListening = false;
            listenTask = Task.Factory.StartNew(Listen, listenCancelToken.Token);
            maintainenceTask = Task.Factory.StartNew(Maintainence, maintainenceCancelToken.Token);

            isStarted = true;
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneMaster"/>.
        /// </summary>
        public void Stop()
        {
            if (!isStarted)
                throw new InvalidOperationException("Cannot stop listening when not started.");

            Log.Logger.Information($"({Program.Configuration.Name}) stopping DFSI control network services, {server.EndPoint}");

            // stop UDP listen task
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

            // stop maintainence task
            if (maintainenceTask != null)
            {
                maintainenceCancelToken.Cancel();

                try
                {
                    maintainenceTask.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) { /* stub */ }
                finally
                {
                    maintainenceCancelToken.Dispose();
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

            while (!abortListening)
            {
                try
                {
                    UdpFrame frame = await server.Receive();
                    if (Program.Configuration.RawPacketTrace)
                        Log.Logger.Debug($"({Program.Configuration.Name}) Network Received (from {frame.Endpoint}) -- {FneUtils.HexDump(frame.Message, 0)}");

                    // decode frame
                    if (frame.Message.Length <= 0)
                        continue;

                    if (reqConnectionToPeer)
                    {
                        // FSC_CONNECT response -- is ... strange
                        if (frame.Message[0] == 1)
                        {
                            reqConnectionToPeer = false;
                            cyclesSinceConnectReq = 0;
                            establishedConnection = true;
                            lastPing = DateTime.Now;

                            FSCConnectResponse resp = new FSCConnectResponse(frame.Message);
                            ConnectResponse?.Invoke(resp);
                        }
                    }
                    else
                    {
                        FSCMessage message = FSCMessage.CreateMessage(frame.Message);
                        switch (message.MessageId)
                        {
                            case MessageType.FSC_ACK:
                                {
                                    FSCACK ackMessage = (FSCACK)message;
                                    switch (ackMessage.ResponseCode)
                                    {
                                        case AckResponseCode.CONTROL_NAK:
                                        case AckResponseCode.CONTROL_NAK_CONNECTED:
                                        case AckResponseCode.CONTROL_NAK_M_UNSUPP:
                                        case AckResponseCode.CONTROL_NAK_V_UNSUPP:
                                        case AckResponseCode.CONTROL_NAK_F_UNSUPP:
                                        case AckResponseCode.CONTROL_NAK_PARMS:
                                        case AckResponseCode.CONTROL_NAK_BUSY:
                                            Log.Logger.Error($"({Program.Configuration.Name}) DFSI ERROR: {ackMessage.AckMessageId} {ackMessage.ResponseCode}");
                                            break;

                                        case AckResponseCode.CONTROL_ACK:
                                            {
                                                if (ackMessage.AckMessageId == MessageType.FSC_DISCONNECT)
                                                {
                                                    reqConnectionToPeer = false;
                                                    cyclesSinceConnectReq = 0;
                                                    establishedConnection = false;
                                                    lastPing = DateTime.Now;
                                                }
                                            }
                                            break;

                                        default:
                                            Log.Logger.Error($"({Program.Configuration.Name}) Unknown DFSI control ack opcode {(byte)ackMessage.AckMessageId} -- {FneUtils.HexDump(frame.Message, 0)}");
                                            break;
                                    }
                                }
                                break;

                            case MessageType.FSC_CONNECT:
                                {
                                    FSCConnectResponse resp = new FSCConnectResponse();
                                    resp.VCBasePort = (ushort)Program.Configuration.LocalRtpPort;

                                    byte[] respBuffer = new byte[resp.Length];
                                    resp.Encode(ref respBuffer);

                                    SendRemote(respBuffer);
                                }
                                break;

                            case MessageType.FSC_DISCONNECT:
                                {
                                    reqConnectionToPeer = false;
                                    cyclesSinceConnectReq = 0;
                                    establishedConnection = false;
                                }
                                break;

                            case MessageType.FSC_HEARTBEAT:
                                {
                                    if (establishedConnection)
                                    {
                                        SendRemote(new FSCACK(MessageType.FSC_HEARTBEAT, AckResponseCode.CONTROL_ACK));
                                        lastPing = DateTime.Now;
                                    }
                                }
                                break;

                            default:
                                Log.Logger.Error($"({Program.Configuration.Name}) Unknown DFSI control opcode {(byte)message.MessageId} -- {FneUtils.HexDump(frame.Message, 0)}");
                                break;
                        }
                    }
                }
                catch (SocketException se)
                {
                    Log.Logger.Fatal($"({Program.Configuration.Name}) SOCKET ERROR: {se.SocketErrorCode}; {se.Message}");
                }

                if (ct.IsCancellationRequested)
                    abortListening = true;
            }
        }

        /// <summary>
        /// Internal maintainence routine.
        /// </summary>
        private async void Maintainence()
        {
            CancellationToken ct = maintainenceCancelToken.Token;
            while (!abortListening)
            {
                if (reqConnectionToPeer)
                    ++cyclesSinceConnectReq;
                if (reqConnectionToPeer && cyclesSinceConnectReq > MAX_CONNECT_WAIT_CYCLES)
                {
                    Log.Logger.Error($"({Program.Configuration.Name}) Remote DFSI host failed to response to FSC_CONNECT. {masterEndpoint}");
                    reqConnectionToPeer = false;
                    cyclesSinceConnectReq = 0;
                }

                if (establishedConnection)
                {
                    SendRemote(new FSCHeartbeat());

                    DateTime dt = lastPing.AddSeconds(Program.Configuration.DfsiHeartbeat * MAX_MISSED_HB);
                    if (dt < DateTime.Now)
                    {
                        Log.Logger.Error($"({Program.Configuration.Name}) Remote DFSI host has timed out. {masterEndpoint}");

                        reqConnectionToPeer = false;
                        cyclesSinceConnectReq = 0;
                        establishedConnection = false;

                        SendRemote(new FSCDisconnect());
                    }
                }

                try
                {
                    await Task.Delay(Program.Configuration.DfsiHeartbeat * 1000, ct);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }
    } // public class ControlService
} // namespace dvmdfsi.DFSI
