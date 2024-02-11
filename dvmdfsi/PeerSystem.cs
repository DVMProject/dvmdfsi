// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023 Bryan Biedenkapp, N2PLL
*
*/
using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using Serilog;

using fnecore;

namespace dvmdfsi
{
    /// <summary>
    /// Implements a peer FNE router system.
    /// </summary>
    public class PeerSystem : FneSystemBase
    {
        protected FnePeer peer;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerSystem"/> class.
        /// </summary>
        public PeerSystem() : base(Create())
        {
            this.peer = (FnePeer)fne;
        }

        /// <summary>
        /// Internal helper to instantiate a new instance of <see cref="FnePeer"/> class.
        /// </summary>
        /// <param name="config">Peer stanza configuration</param>
        /// <returns><see cref="FnePeer"/></returns>
        private static FnePeer Create()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, Program.Configuration.Port);

            if (Program.Configuration.Address == null)
                throw new NullReferenceException("address");
            if (Program.Configuration.Address == string.Empty)
                throw new ArgumentException("address");

            // handle using address as IP or resolving from hostname to IP
            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(Program.Configuration.Address), Program.Configuration.Port);
            }
            catch (FormatException)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(Program.Configuration.Address);
                if (addresses.Length > 0)
                    endpoint = new IPEndPoint(addresses[0], Program.Configuration.Port);
            }

            Log.Logger.Information($"    Peer ID: {Program.Configuration.PeerId}");
            Log.Logger.Information($"    Master Addresss: {Program.Configuration.Address}");
            Log.Logger.Information($"    Master Port: {Program.Configuration.Port}");
            Log.Logger.Information($"    Remote DFSI Addresss: {Program.Configuration.RemoteDfsiAddress}");
            Log.Logger.Information($"    Remote DFSI Control Port: {Program.Configuration.RemoteControlPort}");
            Log.Logger.Information($"    Remote DFSI RTP Port: {Program.Configuration.RemoteRtpPort}");
            string noConnEstablish = (!Program.Configuration.NoConnectionEstablishment) ? "yes" : "no";
            Log.Logger.Information($"    Automatic Connection Establishment: {noConnEstablish}");
            string theManuf = (Program.Configuration.TheManufacturer) ? "yes" : "no";
            Log.Logger.Information($"    \"The\" Manufacturer RTP Packets: {theManuf}");

            FnePeer peer = new FnePeer(Program.Configuration.Name, Program.Configuration.PeerId, endpoint);

            // set configuration parameters
            peer.RawPacketTrace = Program.Configuration.RawPacketTrace;

            peer.PingTime = Program.Configuration.PingTime;
            peer.Passphrase = Program.Configuration.Passphrase;
            peer.Information.Details = ConfigurationObject.ConvertToDetails(Program.Configuration);

            return peer;
        }

        /// <summary>
        /// Helper to send a activity transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendActivityTransfer(string message)
        {
            /* stub */
        }

        /// <summary>
        /// Helper to send a diagnostics transfer message to the master.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendDiagnosticsTransfer(string message)
        {
            /* stub */
        }
    } // public class PeerSystem
} // namespace dvmdfsi
