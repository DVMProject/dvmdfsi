/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
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

using dvmdfsi.FNE;

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

            Encode(ref data);

            FneUtils.WriteBytes(VCBasePort, ref data, 3);                       // Voice Conveyance RTP Port
            FneUtils.WriteBytes(VCSSRC, ref data, 5);                           // Voice Conveyance SSRC
            data[9U] = FSHeartbeatPeriod;                                       // Fixed Station Heartbeat Period
            data[10U] = HostHeartbeatPeriod;                                    // Host Heartbeat Period
        }
    } // public class FSCConnect : FSCMessage
} // namespace dvmdfsi.DFSI.FSC
