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

namespace dvmdfsi.DFSI
{
    /// <summary>
    /// Implements the DFSI control port interface.
    /// </summary>
    public class Control
    {
        private static Random rand = null;

        private bool isStarted = false;

        private UdpListener server = null;
        private IPEndPoint masterEndpoint = null;

        private bool abortListening = false;

        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;
        private CancellationTokenSource maintainenceCancelToken = new CancellationTokenSource();
        private Task maintainenceTask = null;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> for this <see cref="Control"/>.
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
        /// Flag indicating whether this <see cref="Control"/> is running.
        /// </summary>
        public bool IsStarted => isStarted;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="Control"/> class.
        /// </summary>
        static Control()
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
        /// Initializes a new instance of the <see cref="Control"/> class.
        /// </summary>
        public Control() : this(new IPEndPoint(IPAddress.Any, Program.Configuration.LocalControlPort))
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Control"/> class.
        /// </summary>
        /// <param name="endpoint"></param>
        public Control(IPEndPoint endpoint)
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

                    // TODO TODO TODO
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
                // TODO TODO TODO

                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (TaskCanceledException) { /* stub */ }
            }
        }
    } // public class Control
} // namespace dvmdfsi.DFSI
