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

using dvmdfsi.FNE.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 Motorola full rate voice packet.
    /// </summary>
    /// 
    /// Byte 0                   1                   2                   3
    /// Bit  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |  Addtl Data   |  Addtl Data   |  Addtl Data   |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Reserved    |    IMBE 1     |    IMBE 2     |    IMBE 3     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 4     |    IMBE 5     |    IMBE 6     |    IMBE 7     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 8     |    IMBE 9     |    IMBE 10    |    IMBE 11    |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //      |    Src Flag   |
    ///     +=+=+=+=+=+=+=+=+
    public class MotFullRateVoice
    {
        public const int LENGTH = 17;
        public const int SHORTENED_LENGTH = 14;
        private const int IMBE_BUF_LEN = 11;

        /// <summary>
        /// Frame type.
        /// </summary>
        public byte FrameType
        {
            get;
            set;
        }

        /// <summary>
        /// IMBE.
        /// </summary>
        public byte[] IMBE
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] AdditionalFrameData
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

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MotFullRateVoice"/> class.
        /// </summary>
        public MotFullRateVoice()
        {
            FrameType = P25DFSI.P25_DFSI_LDU1_VOICE1;
            AdditionalFrameData = null;
            Source = 0;

            IMBE = new byte[IMBE_BUF_LEN];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotFullRateVoice"/> class.
        /// </summary>
        /// <param name="data"></param>
        public MotFullRateVoice(byte[] data) : this()
        {
            Decode(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int Size()
        {
            if (AdditionalFrameData != null)
                return LENGTH + AdditionalFrameData.Length;
            else
            {
                if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE1 ||  FrameType == P25DFSI.P25_DFSI_LDU1_VOICE2 ||
                    FrameType == P25DFSI.P25_DFSI_LDU2_VOICE10 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE11)
                    return SHORTENED_LENGTH;
            }

            return LENGTH;
        }

        /// <summary>
        /// Decode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="shortened"></param>
        /// <returns></returns>
        public bool Decode(byte[] data, bool shortened = false)
        {
            if (data == null)
                return false;

            IMBE = new byte[IMBE_BUF_LEN];

            FrameType = (byte)(data[0U] & 0xFFU);                               // Frame Type
            if (shortened)
            {
                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    IMBE[i] = data[i + 1U];                                     // IMBE

                Source = data[12U];
            }
            else
            {
                AdditionalFrameData = new byte[4];
                Buffer.BlockCopy(data, 1, AdditionalFrameData, 0, AdditionalFrameData.Length);

                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    IMBE[i] = data[i + 5U];                                     // IMBE

                Source = data[16U];
            }

            return true;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="shortened"></param>
        public void Encode(ref byte[] data, bool shortened = false)
        {
            if (data == null)
                return;

            data[0U] = FrameType;                                               // Frame Type
            if (shortened)
            {
                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    data[i + 1U] = IMBE[i];                                     // IMBE

                data[12U] = Source;
            }
            else
            {
                if (AdditionalFrameData != null)
                {
                    if (AdditionalFrameData.Length >= 4)
                        Buffer.BlockCopy(AdditionalFrameData, 0, data, 1, 4);
                }

                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    data[i + 5U] = IMBE[i];                                     // IMBE

                data[16U] = Source;
            }
        }
    } // public class MotFullRateVoice
} // namespace dvmdfsi.DFSI.RTP
