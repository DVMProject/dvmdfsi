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
    public class FSCConnectResponse : FSCResponse
    {
        /// <summary>
        /// Length of response.
        /// </summary>
        public override uint Length { get => 3; }

        /// <summary>
        /// Voice Conveyance RTP Port.
        /// </summary>
        public ushort VCBasePort
        {
            get;
            set;
        }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCResponse"/> class.
        /// </summary>
        public FSCConnectResponse() : base()
        {
            VCBasePort = (ushort)Program.Configuration.LocalRtpPort;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCConnectResponse"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FSCConnectResponse(byte[] data) : this()
        {
            Decode(data);
        }

        /// <summary>
        /// Decode a FSC connect response.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            base.Decode(data);

            VCBasePort = FneUtils.ToUInt16(data, 1);                            // Voice Conveyance RTP Port

            return true;
        }

        /// <summary>
        /// Encode a FSC connect response.
        /// </summary>
        /// <param name="data"></param>
        public override void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            base.Encode(ref data);

            FneUtils.WriteBytes(VCBasePort, ref data, 1);                       // Voice Conveyance RTP Port
        }
    } // public class FSCConnectResponse : FSCResponse
} // namespace dvmdfsi.DFSI.FSC
