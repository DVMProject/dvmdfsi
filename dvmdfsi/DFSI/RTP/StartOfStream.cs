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
