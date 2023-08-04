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

using dvmdfsi.FNE;
using dvmdfsi.FNE.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 Motorola voice header frame 2.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   TGID                        | Reserved                      |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |                               |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class MotVoiceHeader2
    {
        public const int LENGTH = 22;

        /// <summary>
        /// 
        /// </summary>
        public ushort TGID
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MotVoiceHeader2"/> class.
        /// </summary>
        public MotVoiceHeader2()
        {
            TGID = 0;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotVoiceHeader2"/> class.
        /// </summary>
        /// <param name="data"></param>
        public MotVoiceHeader2(byte[] data)
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

            TGID = FneUtils.ToUInt16(data, 1);

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

            data[0U] = P25DFSI.P25_DFSI_MOT_VHDR_1;
            FneUtils.WriteBytes(TGID, ref data, 1);
        }
    } // public class MotVoiceHeader2
} // namespace dvmdfsi.DFSI.RTP
