/**
* Digital Voice Modem - ISSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / ISSI
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
    /// DFSI Block Types
    /// </summary>
    public enum BlockType : byte
    {
        FULL_RATE_VOICE = 0,

        VOICE_HEADER_P1 = 6,
        VOICE_HEADER_P2 = 7,

        START_OF_STREAM = 9,
        END_OF_STREAM = 10,

        UNDEFINED = 127
    } // public enum BlockType

    /// <summary>
    /// Implements a DFSI block header packet.
    /// </summary>
    /// 
    /// Compact Form
    /// Byte 0
    /// Bit  0 1 2 3 4 5 6 7 
    ///     +-+-+-+-+-+-+-+-+
    ///     |E|      BT     |
    ///     +-+-+-+-+-+-+-+-+
    /// 
    /// Verbose Form
    /// Byte 0                   1                   2                   3   
    /// Bit  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |E|      BT     |             TSO           |         BL        |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    public class BlockHeader
    {
        public const int LENGTH = 1;
        public const int VERBOSE_LENGTH = 4;

        /// <summary>
        /// Payload type.
        /// </summary>
        /// <remarks>
        /// This simple boolean marks this header as either IANA standard, or profile specific.
        /// </remarks>
        public bool PayloadType
        {
            get;
            set;
        }

        /// <summary>
        /// Block type.
        /// </summary>
        public BlockType Type
        {
            get;
            set;
        }

        /// <summary>
        /// Timestamp Offset.
        /// </summary>
        public uint TimestampOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Length of corresponding block.
        /// </summary>
        public uint BlockLength
        {
            get;
            set;
        }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockHeader"/> class.
        /// </summary>
        public BlockHeader()
        {
            PayloadType = false;
            Type = BlockType.UNDEFINED;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockHeader"/> class.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="verbose"></param>
        public BlockHeader(byte[] data, bool verbose = false) : this()
        {
            Decode(data, verbose);
        }

        /// <summary>
        /// Decode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public bool Decode(byte[] data, bool verbose = false)
        {
            if (data == null)
                return false;

            ulong value = 0U;

            // combine bytes into ulong (8 byte) value
            value = data[0U];
            value = (value << 8) + data[1U];
            value = (value << 8) + data[2U];
            value = (value << 8) + data[3U];

            PayloadType = (data[0U] & 0x80) == 0x80;                            // Payload Type
            Type = (BlockType)(data[0U] & 0x7F);                                // Block Type

            if (verbose)
            {
                TimestampOffset = (uint)((value >> 10) & 0x3FF);                // Timestamp Offset
                BlockLength = (uint)(value & 0x3FF);                            // Block Length
            }

            return true;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="verbose"></param>
        public void Encode(ref byte[] data, bool verbose = false)
        {
            if (data == null)
                return;

            if (!verbose)
            {
                data[0U] = (byte)((PayloadType ? 0x80U : 0x00U) +               // Payload Type
                    ((byte)Type & 0x7FU));                                      // Block Type
            }
            else
            {
                ulong value = 0;

                value = (ulong)((PayloadType ? 0x80U : 0x00U) +                 // Payload Type
                    ((byte)Type & 0x7FU));                                      // Block Type
                value = (value << 24) + (ulong)(TimestampOffset & 0x3FFU);      // Timestamp Offset
                value = (value << 10) + (ulong)(BlockLength & 0x3FFU);          // Block Length

                // split ulong (8 byte) value into bytes
                data[0U] = (byte)((value >> 24) & 0xFFU);
                data[1U] = (byte)((value >> 16) & 0xFFU);
                data[2U] = (byte)((value >> 8) & 0xFFU);
                data[3U] = (byte)((value >> 0) & 0xFFU);
            }
        }
    } // public class BlockHeader
} // namespace dvmdfsi.DFSI.RTP
