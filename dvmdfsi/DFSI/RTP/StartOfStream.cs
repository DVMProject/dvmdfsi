﻿// SPDX-License-Identifier: AGPL-3.0-only
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

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 DFSI start of stream packet.
    /// </summary>
    /// 
    /// Byte 0               1               2
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |              NID              | Rsvd  | Err C |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class StartOfStream
    {
        public const int LENGTH = 4;

        /// <summary>
        /// Network Identifier.
        /// </summary>
        public ushort NID
        {
            get;
            set;
        }

        /// <summary>
        /// Error count.
        /// </summary>
        public byte ErrorCount
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="StartOfStream"/> class.
        /// </summary>
        public StartOfStream()
        {
            NID = 0;
            ErrorCount = 0;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="StartOfStream"/> class.
        /// </summary>
        /// <param name="data"></param>
        public StartOfStream(byte[] data)
        {
            Decode(data);
        }

        /// <summary>
        /// Decode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            NID = FneUtils.ToUInt16(data, 0);                                   // Network Identifier
            ErrorCount = (byte)(data[2U] & 0x0FU);                              // Error Count

            return true;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        public void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            FneUtils.WriteBytes(NID, ref data, 0);                              // Network Identifier
            data[2U] = (byte)(ErrorCount & 0x0FU);                              // Error Count
        }
    } // public class StartOfStream
} // namespace dvmdfsi.DFSI.RTP
