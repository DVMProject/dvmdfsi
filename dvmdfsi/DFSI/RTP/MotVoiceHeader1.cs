﻿/**
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
using fnecore.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 Motorola voice header frame 1.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Encoded Motorola Start of Stream                            |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   ICW Flag ?  |     RSSI      |  RSSI Valid   |     RSSI      |
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
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     | Src Flag      |
    ///     +-+-+-+-+-+-+-+-+
    public class MotVoiceHeader1
    {
        public const int LENGTH = 30;
        public const int HCW_LENGTH = 20;

        /// <summary>
        /// 
        /// </summary>
        public byte ICW
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte RSSI
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public RSSIValidityFlag RSSIValidity
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public MotStartOfStream StartOfStream
        {
            get;
            set;
        }

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
        /// Initializes a new instance of the <see cref="MotVoiceHeader1"/> class.
        /// </summary>
        public MotVoiceHeader1()
        {
            ICW = 0;
            RSSI = 0;
            RSSIValidity = RSSIValidityFlag.INVALID;
            Source = 0x02;

            StartOfStream = null;
            Header = new byte[HCW_LENGTH];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotVoiceHeader1"/> class.
        /// </summary>
        /// <param name="data"></param>
        public MotVoiceHeader1(byte[] data)
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

            // decode start of stream data
            StartOfStream = new MotStartOfStream();
            {
                byte[] buffer = new byte[MotStartOfStream.LENGTH];
                Buffer.BlockCopy(data, 1, buffer, 0, 4);
                StartOfStream.Decode(buffer);
            }

            ICW = data[5U];                                                     // ICW Flag ?
            RSSI = data[6U];                                                    // RSSI
            RSSIValidity = (RSSIValidityFlag)data[7U];                          // RSSI Validity
            Source = data[29];

            Header = new byte[HCW_LENGTH];
            Buffer.BlockCopy(data, 10, Header, 0, HCW_LENGTH);

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
            if (StartOfStream == null)
                return;

            data[0U] = P25DFSI.P25_DFSI_MOT_VHDR_1;

            // encode start of stream data
            if (StartOfStream != null)
            {
                byte[] buffer = new byte[MotStartOfStream.LENGTH];
                StartOfStream.Encode(ref buffer);
                Buffer.BlockCopy(buffer, 1, data, 1, MotStartOfStream.LENGTH - 1);
            }

            data[5U] = ICW;                                                     // ICW Flag ?
            data[6U] = RSSI;                                                    // RSSI
            data[7U] = (byte)RSSIValidity;                                      // RSSI Validity
            data[8U] = RSSI;                                                    // RSSI

            if (Header != null)
                if (Header.Length == HCW_LENGTH)
                    Buffer.BlockCopy(Header, 0, data, 9, HCW_LENGTH);

            data[MotVoiceHeader1.LENGTH - 1] = Source;
        }
    } // public class MotVoiceHeader1
} // namespace dvmdfsi.DFSI.RTP
