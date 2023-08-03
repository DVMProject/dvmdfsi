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
