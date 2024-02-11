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
*   Copyright (C) 2023 Patrick McDonnell, W3AXL
*
*/
using System;

using fnecore;
using fnecore.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 Motorola voice header frame 2.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Header Control Word                                         |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |               | Reserved      |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class MotVoiceHeader2
    {
        public const int LENGTH = 22;
        public const int HCW_LENGTH = 20;

        /// <summary>
        /// 
        /// </summary>
        public byte Source
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Header
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
            Source = 0x02;

            Header = new byte[HCW_LENGTH];
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

            Source = data[21];

            Header = new byte[HCW_LENGTH];
            Buffer.BlockCopy(data, 1, Header, 0, HCW_LENGTH);

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

            data[0U] = P25DFSI.P25_DFSI_MOT_VHDR_2;

            if (Header != null)
                if (Header.Length == HCW_LENGTH)
                    Buffer.BlockCopy(Header, 0, data, 1, HCW_LENGTH);

            data[MotVoiceHeader2.LENGTH - 1] = Source;
        }
    } // public class MotVoiceHeader2
} // namespace dvmdfsi.DFSI.RTP
