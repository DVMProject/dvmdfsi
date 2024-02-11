// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2023 Bryan Biedenkapp, N2PLL
*
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

using fnecore;
using fnecore.P25;

namespace dvmdfsi.DFSI
{
    /// <summary>
    /// Callback used to process a RTP network frame.
    /// </summary>
    /// <param name="frame"><see cref="UdpFrame"/></param>
    /// <param name="message"></param>
    /// <param name="rtpHeader"></param>
    /// <returns>True, if the frame was handled, otherwise false.</returns>
    public delegate void RTPNetworkFrame(UdpFrame frame, byte[] message, RtpHeader rtpHeader);

    /// <summary>
    /// Implements the DFSI RTP port interface
    /// </summary>
    public class RTPService
    {
        private static Random rand = null;

        private bool isStarted = false;

        private UdpListener server = null;
        private IPEndPoint masterEndpoint = null;

        private bool abortListening = false;

        private CancellationTokenSource listenCancelToken = new CancellationTokenSource();
        private Task listenTask = null;

        private ushort currPktSeq = 0;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> for this <see cref="RTP"/>.
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
        static RTPService()
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
        /// Initializes a new instance of the <see cref="RTP"/> class.
        /// </summary>
        public RTPService() : this(new IPEndPoint(IPAddress.Any, Program.Configuration.LocalRtpPort))
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RTP"/> class.
        /// </summary>
        /// <param name="endpoint"></param>
        public RTPService(IPEndPoint endpoint)
        {
            server = new UdpListener(endpoint);

            // handle using address as IP or resolving from hostname to IP
            try
            {
                masterEndpoint = new IPEndPoint(IPAddress.Parse(Program.Configuration.RemoteDfsiAddress), Program.Configuration.RemoteRtpPort);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Program.Configuration.RemoteDfsiAddress);
                if (addresses.Length > 0)
                    masterEndpoint = new IPEndPoint(addresses[0], Program.Configuration.RemoteRtpPort);
            }
        }

        /// <summary>
        /// Helper to reset the master endpoint.
        /// </summary>
        /// <param name="port"></param>
        public void ResetMasterEndpoint(ushort port)
        {
            if (port != Program.Configuration.RemoteRtpPort)
                return;

            // handle using address as IP or resolving from hostname to IP
            try
            {
                masterEndpoint = new IPEndPoint(IPAddress.Parse(Program.Configuration.RemoteDfsiAddress), port);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Program.Configuration.RemoteDfsiAddress);
                if (addresses.Length > 0)
                    masterEndpoint = new IPEndPoint(addresses[0], port);
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
        /// Helper to read and process a DFSI RTP frame.
        /// </summary>
        /// <param name="frame">Raw UDP socket frame.</param>
        /// <param name="messageLength">Length of payload message.</param>
        /// <param name="rtpHeader">RTP Header.</param>
        private byte[] ReadFrame(UdpFrame frame, out int messageLength, out RtpHeader rtpHeader)
        {
            int length = frame.Message.Length;
            messageLength = -1;
            rtpHeader = null;

            // read message from socket
            if (length > 0)
            {
                if (length < Constants.RtpHeaderLengthBytes)
                {
                    Log.Logger.Error($"Message received from network is malformed! " +
                        $"{Constants.RtpHeaderLengthBytes} bytes != {frame.Message.Length} bytes");
                    return null;
                }

                // decode RTP header
                rtpHeader = new RtpHeader();
                if (!rtpHeader.Decode(frame.Message))
                {
                    Log.Logger.Error($"Invalid RTP packet received from network");
                    return null;
                }

                // ensure payload type is correct
                if (rtpHeader.PayloadType != P25DFSI.P25_RTP_PAYLOAD_TYPE)
                {
                    Log.Logger.Error("Invalid RTP header received from network");
                    return null;
                }

                // copy message
                messageLength = (int)(frame.Message.Length - Constants.RtpHeaderLengthBytes);
                byte[] message = new byte[messageLength];
                Buffer.BlockCopy(frame.Message, (int)(Constants.RtpHeaderLengthBytes), message, 0, messageLength);

                return message;
            }

            return null;
        }

        /// <summary>
        /// Helper to generate and write a DFSI RTP frame.
        /// </summary>
        /// <param name="message">Payload message.</param>
        /// <param name="pktSeq">RTP Packet Sequence.</param>
        /// <returns></returns>
        private byte[] WriteFrame(byte[] message, ushort pktSeq)
        {
            byte[] buffer = new byte[message.Length + Constants.RtpHeaderLengthBytes];
            FneUtils.Memset(buffer, 0, buffer.Length);

            RtpHeader header = new RtpHeader();
            header.Extension = false;
            header.PayloadType = P25DFSI.P25_RTP_PAYLOAD_TYPE;
            header.Sequence = pktSeq;
            header.SSRC = Program.Configuration.PeerId;

            header.Encode(ref buffer);

            Buffer.BlockCopy(message, 0, buffer, (int)(Constants.RtpHeaderLengthBytes), message.Length);
            return buffer;
        }

        /// <summary>
        /// Helper to send a raw message to the master.
        /// </summary>
        /// <param name="message">Byte array containing message to send</param>
        /// <param name="pktSeq">RTP Packet Sequence</param>
        public void SendRemote(byte[] message, ushort pktSeq)
        {
            Send(new UdpFrame()
            {
                Endpoint = masterEndpoint,
                Message = WriteFrame(message, pktSeq)
            });
        }

        /// <summary>
        /// Helper to send a raw message to the master.
        /// </summary>
        /// <param name="message">Byte array containing message to send</param>
        public void SendRemote(byte[] message)
        {
            SendRemote(message, pktSeq());
        }

        /// <summary>
        /// Helper to update the RTP packet sequence.
        /// </summary>
        /// <param name="reset"></param>
        /// <returns>RTP packet sequence.</returns>
        public ushort pktSeq(bool reset = false)
        {
            if (reset)
            {
                currPktSeq = 0;
                return currPktSeq;
            }

            ushort curr = currPktSeq;
            ++currPktSeq;
            if (currPktSeq > ushort.MaxValue)
                currPktSeq = 0;

            return curr;
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

                    // decode RTP frame
                    if (frame.Message.Length <= 0)
                        continue;

                    RtpHeader rtpHeader;
                    int messageLength = 0;
                    byte[] message = ReadFrame(frame, out messageLength, out rtpHeader);
                    if (message == null)
                    {
                        Log.Logger.Error($"({Program.Configuration.Name}) Malformed packet (from {frame.Endpoint}); failed to decode RTP frame");
                        continue;
                    }

                    if (message.Length < 1)
                    {
                        Log.Logger.Error($"({Program.Configuration.Name}) Malformed packet (from {frame.Endpoint}) -- {FneUtils.HexDump(message, 0)}");
                        continue;
                    }

                    // validate frame endpoint
                    if (frame.Endpoint.ToString() == masterEndpoint.ToString())
                        RTPFrameHandler?.Invoke(frame, message, rtpHeader);
                }
                catch (SocketException se)
                {
                    Log.Logger.Fatal($"({Program.Configuration.Name}) SOCKET ERROR: {se.SocketErrorCode}; {se.Message}");
                }

                if (ct.IsCancellationRequested)
                    abortListening = true;
            }
        }
    } // public class RTPService
} // namespace dvmdfsi.DFSI
