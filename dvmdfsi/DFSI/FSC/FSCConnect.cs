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

using fnecore;

namespace dvmdfsi.DFSI.FSC
{
    /// <summary>
    /// 
    /// </summary>
    public class FSCConnect : FSCMessage
    {
        /// <summary>
        /// Length of message.
        /// </summary>
        public override uint Length { get => 11; }

        /// <summary>
        /// Voice Conveyance RTP Port.
        /// </summary>
        public ushort VCBasePort
        {
            get;
            set;
        }

        /// <summary>
        /// SSRC Identifier for all RTP transmissions.
        /// </summary>
        public uint VCSSRC
        {
            get;
            set;
        }

        /// <summary>
        /// Fixed Station Heartbeat Period.
        /// </summary>
        public byte FSHeartbeatPeriod
        {
            get;
            set;
        }

        /// <summary>
        /// Host Heartbeat Period.
        /// </summary>
        public byte HostHeartbeatPeriod
        {
            get;
            set;
        }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCConnect"/> class.
        /// </summary>
        public FSCConnect() : base()
        {
            VCBasePort = (ushort)Program.Configuration.LocalRtpPort;
            VCSSRC = Program.Configuration.PeerId;
            FSHeartbeatPeriod = 5;
            HostHeartbeatPeriod = 5;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCConnect"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FSCConnect(byte[] data) : this()
        {
            Decode(data);
        }

        /// <summary>
        /// Decode a FSC connect packet.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            base.Decode(data);

            VCBasePort = FneUtils.ToUInt16(data, 3);                            // Voice Conveyance RTP Port
            VCSSRC = FneUtils.ToUInt32(data, 5);                                // Voice Conveyance SSRC
            FSHeartbeatPeriod = data[9U];                                       // Fixed Station Heartbeat Period
            HostHeartbeatPeriod = data[10U];                                    // Host Heartbeat Period

            return true;
        }

        /// <summary>
        /// Encode a FSC connect packet.
        /// </summary>
        /// <param name="data"></param>
        public override void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            base.Encode(ref data);

            FneUtils.WriteBytes(VCBasePort, ref data, 3);                       // Voice Conveyance RTP Port
            FneUtils.WriteBytes(VCSSRC, ref data, 5);                           // Voice Conveyance SSRC
            data[9U] = FSHeartbeatPeriod;                                       // Fixed Station Heartbeat Period
            data[10U] = HostHeartbeatPeriod;                                    // Host Heartbeat Period
        }
    } // public class FSCConnect : FSCMessage
} // namespace dvmdfsi.DFSI.FSC
