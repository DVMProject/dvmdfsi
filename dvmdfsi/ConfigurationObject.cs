// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2023 Patrick McDonnell, W3AXL
*
*/
using System;
using System.Collections.Generic;

using fnecore;

namespace dvmdfsi
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigLogObject
    {
        /// <summary>
        /// 
        /// </summary>
        public int DisplayLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public int FileLevel = 1;
        /// <summary>
        /// 
        /// </summary>
        public string FilePath = ".";
        /// <summary>
        /// 
        /// </summary>
        public string FileRoot = "dvmdfsi";
    } // public class ConfigLogObject

    /// <summary>
    /// Enum for valid DVMDFSI modes
    /// </summary>
    public enum DFSIMode
    {
        None = 0,
        UdpDvm = 1,
        SerialDvm = 2,
        SerialUdp = 3
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConfigurationObject
    {
        /// <summary>
        /// 
        /// </summary>
        public ConfigLogObject Log = new ConfigLogObject();

        /// <summary>
        /// 
        /// </summary>
        public int PingTime = 5;

        /// <summary>
        /// 
        /// </summary>
        public bool RawPacketTrace = false;

        /// <summary>
        /// Mode for DFSI translation (1 - UDP DFSI to DVM FNE, 2 - Serial DFSI to DVM FNE, 3 - Serial DFSI to UDP DFSI)
        /// </summary>
        public DFSIMode Mode = 0;

        /// <summary>
        /// 
        /// </summary>
        public string Name = "ISSI";
        /// <summary>
        /// 
        /// </summary>
        public uint PeerId;
        /// <summary>
        /// 
        /// </summary>
        public string Address;
        /// <summary>
        /// 
        /// </summary>
        public int Port;
        /// <summary>
        /// 
        /// </summary>
        public string Passphrase;

        /// <summary>
        /// 
        /// </summary>
        public int DfsiHeartbeat = 5;
        /// <summary>
        /// 
        /// </summary>
        public bool NoConnectionEstablishment = false;
        /// <summary>
        /// 
        /// </summary>
        public bool TheManufacturer = false;

        /// <summary>
        /// 
        /// </summary>
        public int LocalControlPort = 27000;
        /// <summary>
        /// 
        /// </summary>
        public int LocalRtpPort = 27500;

        /// <summary>
        /// 
        /// </summary>
        public string RemoteDfsiAddress;
        /// <summary>
        /// 
        /// </summary>
        public int RemoteControlPort = 27000;
        /// <summary>
        /// 
        /// </summary>
        public int RemoteRtpPort = 27500;
        /// <summary>
        /// 
        /// </summary>
        public string SerialPortName = "";
        /// <summary>
        /// 
        /// </summary>
        public int SerialBaudrate = 0;
        /// <summary>
        /// 
        /// </summary>
        public int SerialTxJitter = 100;

        /*
        ** Methods
        */

        /// <summary>
        /// Helper to convert the <see cref="ConfigPeerObject"/> to a <see cref="PeerDetails"/> object.
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public static PeerDetails ConvertToDetails(ConfigurationObject peer)
        {
            PeerDetails details = new PeerDetails();

            // identity
            details.Identity = peer.Name;
            details.RxFrequency = 0;
            details.TxFrequency = 0;

            // system info
            details.Latitude = 0.0d;
            details.Longitude = 0.0d;
            details.Height = 1;
            details.Location = "Digital Network";

            // channel data
            details.TxPower = 0;
            details.TxOffsetMhz = 0.0f;
            details.ChBandwidthKhz = 0.0f;
            details.ChannelID = 0;
            details.ChannelNo = 0;

            // RCON
            details.Password = "ABCD123";
            details.Port = 9990;

            details.Software = AssemblyVersion._VERSION;

            return details;
        }
    } // public class ConfigurationObject
} // namespace dvmdfsi
