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
using Serilog;

using fnecore.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 Motorola full rate voice packet.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
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
        public const int SHORTENED_LENGTH = 13;
        public const int ADDTL_LENGTH = 4;
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
            Source = 0x02;

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
            int length = 0;
            
            //if (AdditionalFrameData != null)
            //    length += AdditionalFrameData.Length;

            if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE1 ||  FrameType == P25DFSI.P25_DFSI_LDU1_VOICE2 ||
                FrameType == P25DFSI.P25_DFSI_LDU2_VOICE10 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE11)
                length += SHORTENED_LENGTH;
            else
                length += LENGTH;
            
            // these ones are the weird ones
            if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE9 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE18)
                length -= 1;

            return length;
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

            Log.Logger.Debug($"({Program.Configuration.Name}) decoding {(shortened ? "shortened " : "")}Mot voice frame {BitConverter.ToString(data).Replace("-"," ")}");

            IMBE = new byte[IMBE_BUF_LEN];

            FrameType = (byte)(data[0U] & 0xFFU);                               // Frame Type
            if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE2 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE11)
                shortened = true;

            if (shortened)
            {
                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    IMBE[i] = data[i + 1U];                                     // IMBE

                Source = data[12U];
            }
            else
            {
                // Frames 0x6A and 0x73 are missing the 0x00 padding byte, so we start IMBE data 1 byte earlier
                uint IMBEStart = 5U;
                if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE9 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE18)
                {
                    IMBEStart = 4U;
                }

                AdditionalFrameData = new byte[4];
                Buffer.BlockCopy(data, 1, AdditionalFrameData, 0, AdditionalFrameData.Length);

                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    IMBE[i] = data[i + IMBEStart];                                     // IMBE

                Source = data[11 + IMBEStart];
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
            if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE2 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE11)
                shortened = true;

            if (shortened)
            {
                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    data[i + 1U] = IMBE[i];                                     // IMBE

                data[12U] = Source;
            }
            else
            {
                // Frames 0x6A and 0x73 are missing the 0x00 padding byte, so we start IMBE data 1 byte earlier
                uint IMBEStart = 5U;
                if (FrameType == P25DFSI.P25_DFSI_LDU1_VOICE9 || FrameType == P25DFSI.P25_DFSI_LDU2_VOICE18)
                {
                    IMBEStart = 4U;
                }

                if (AdditionalFrameData != null)
                {
                    if (AdditionalFrameData.Length >= 4)
                        Buffer.BlockCopy(AdditionalFrameData, 0, data, 1, 4);
                }

                for (int i = 0; i < IMBE_BUF_LEN; i++)
                    data[i + IMBEStart] = IMBE[i];                                     // IMBE

                data[11 + IMBEStart] = Source;
            }
        }
    } // public class MotFullRateVoice
} // namespace dvmdfsi.DFSI.RTP
