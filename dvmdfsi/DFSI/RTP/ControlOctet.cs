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

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a DFSI control octet packet.
    /// </summary>
    /// 
    /// Byte 0
    /// Bit  0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+
    ///     |S|C|   BHC     |
    ///     +-+-+-+-+-+-+-+-+
    public class ControlOctet
    {
        public const int LENGTH = 1;

        /// <summary>
        /// 
        /// </summary>
        public bool Signal
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates a compact (1) or verbose (0) block header.
        /// </summary>
        public bool Compact
        {
            get;
            set;
        }

        /// <summary>
        /// Number of block headers following this control octet.
        /// </summary>
        public byte BlockHeaderCount
        {
            get;
            set;
        }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlOctet"/> class.
        /// </summary>
        public ControlOctet()
        {
            Signal = false;
            Compact = true;
            BlockHeaderCount = 0;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlOctet"/> class.
        /// </summary>
        /// <param name="data"></param>
        public ControlOctet(byte data) : this()
        {
            Decode(data);
        }

        /// <summary>
        /// Decode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Decode(byte data)
        {
            Signal = (data & 0x07) == 0x07;                                     // Signal Flag
            Compact = (data & 0x06) == 0x06;                                    // Compact Flag
            BlockHeaderCount = (byte)(data & 0x3F);                             // Block Header Count

            return true;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        public void Encode(ref byte data)
        {
            data = (byte)((Signal ? 0x07U : 0x00U) +                            // Signal Flag
                (Compact ? 0x06U : 0x00U) +                                     // Control Flag
                (BlockHeaderCount & 0x3F));
        }
    } // public class ControlOctet
} // namespace dvmdfsi.DFSI.RTP
