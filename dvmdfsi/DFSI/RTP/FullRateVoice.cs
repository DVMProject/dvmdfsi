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

using fnecore.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// Implements a P25 full rate voice packet.
    /// </summary>
    /// 
    /// CAI Frames 1, 2, 10 and 11.
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |       U0(b11-0)       |      U1(b11-0)        |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |        U2(b10-0)      |      U3(b11-0)        |   U4(b10-3)   |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |  U4 |     U5(b10-0)       |     U6(b10-0)       |  U7(b6-0)   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B |
    ///     |     |     | | |4|     |   |   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    /// 
    /// CAI Frames 3 - 8.
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |       U0(b11-0)       |      U1(b11-0)        |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |        U2(b10-0)      |      U3(b11-0)        |   U4(b10-3)   |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |  U4 |     U5(b10-0)       |     U6(b10-0)       |  U7(b6-0)   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B | LC0,4,8   | LC1,5,9   | LC2,  |
    ///     |     |     | | |4|     |   |   |           |           | 6,10  |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |   | LC3,7,11  |R| Status      |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    /// 
    /// CAI Frames 12 - 17.
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |       U0(b11-0)       |      U1(b11-0)        |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |        U2(b10-0)      |      U3(b11-0)        |   U4(b10-3)   |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |  U4 |     U5(b10-0)       |     U6(b10-0)       |  U7(b6-0)   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B | ES0,4,8   | ES1,5,9   | ES2,  |
    ///     |     |     | | |4|     |   |   |           |           | 6,10  |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |   | ES3,7,11  |R| Status      |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///
    /// CAI Frames 9 and 10.
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |       U0(b11-0)       |      U1(b11-0)        |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |        U2(b10-0)      |      U3(b11-0)        |   U4(b10-3)   |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |  U4 |     U5(b10-0)       |     U6(b10-0)       |  U7(b6-0)   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B | LSD0,2        | LSD1,3        |
    ///     |     |     | | |4|     |   |   |               |               |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     | Rsvd  |Si |Sj |
    ///     +=+=+=+=+=+=+=+=+
    /// 
    /// Because the TIA.102-BAHA spec represents the "message vectors" as 
    /// 16-bit units (U0 - U7) this makes understanding the layout of the 
    /// buffer ... difficult for the 8-bit aligned minded. The following is
    /// the layout with 8-bit aligned IMBE blocks instead of message vectors:
    /// 
    /// CAI Frames 1, 2, 10 and 11.
    ///
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |    IMBE 1     |    IMBE 2     |    IMBE 3     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 4     |    IMBE 5     |    IMBE 6     |    IMBE 7     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 8     |    IMBE 9     |    IMBE 10    |    IMBE 11    |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B |
    ///     |     |     | | |4|     |   |   |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    /// 
    /// CAI Frames 3 - 8.
    ///
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |    IMBE 1     |    IMBE 2     |    IMBE 3     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 4     |    IMBE 5     |    IMBE 6     |    IMBE 7     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 8     |    IMBE 9     |    IMBE 10    |    IMBE 11    |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B |    Link Ctrl  |    Link Ctrl  |
    ///     |     |     | | |4|     |   |   |               |               |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |    Link Ctrl  |R| Status      |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    /// 
    /// CAI Frames 12 - 17.
    ///
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |    IMBE 1     |    IMBE 2     |    IMBE 3     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 4     |    IMBE 5     |    IMBE 6     |    IMBE 7     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 8     |    IMBE 9     |    IMBE 10    |    IMBE 11    |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B |    Enc Sync   |    Enc Sync   |
    ///     |     |     | | |4|     |   |   |               |               |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |    Enc Sync   |R| Status      |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    /// 
    /// CAI Frames 9 and 10.
    ///
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |       FT      |    IMBE 1     |    IMBE 2     |    IMBE 3     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 4     |    IMBE 5     |    IMBE 6     |    IMBE 7     |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |    IMBE 8     |    IMBE 9     |    IMBE 10    |    IMBE 11    |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     |  Et | Er  |M|L|E|  E1 |SF | B | LSD0,2        | LSD1,3        |
    ///     |     |     | | |4|     |   |   |               |               |
    ///     +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
    ///     | Rsvd  |Si |Sj |
    ///     +=+=+=+=+=+=+=+=+
    public class FullRateVoice
    {
        public const int LENGTH = 14;
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
        /// Total errors detected in the frame.
        /// </summary>
        public byte TotalErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Flag indicating the frame should be muted.
        /// </summary>
        public bool MuteFrame
        {
            get;
            set;
        }

        /// <summary>
        /// Flag indicating the frame was lost.
        /// </summary>
        public bool LostFrame
        {
            get;
            set;
        }

        /// <summary>
        /// Superframe Counter.
        /// </summary>
        public byte SuperFrameCnt
        {
            get;
            set;
        }

        /// <summary>
        /// Busy status.
        /// </summary>
        public byte Busy
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

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FullRateVoice"/> class.
        /// </summary>
        public FullRateVoice()
        {
            FrameType = P25DFSI.P25_DFSI_LDU1_VOICE1;
            TotalErrors = 0;
            MuteFrame = false;
            LostFrame = false;
            SuperFrameCnt = 0;
            Busy = 0;
            AdditionalFrameData = null;

            IMBE = new byte[IMBE_BUF_LEN];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FullRateVoice"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FullRateVoice(byte[] data) : this()
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
                return LENGTH;
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

            IMBE = new byte[IMBE_BUF_LEN];

            FrameType = (byte)(data[0U] & 0xFFU);                               // Frame Type
            for (int i = 0; i < IMBE_BUF_LEN; i++)
                IMBE[i] = data[i + 1U];                                         // IMBE

            TotalErrors = (byte)((data[12U] >> 5) & 0x07U);                     // Total Errors
            MuteFrame = (data[12U] & 0x02U) == 0x02U;                           // Mute Frame Flag
            LostFrame = (data[12U] & 0x01U) == 0x01U;                           // Lost Frame Flag
            SuperFrameCnt = (byte)((data[13U] >> 2) & 0x03U);                   // Superframe Counter
            Busy = (byte)(data[13U] & 0x03U);

            // extract additional frame data
            if (data.Length > LENGTH)
            {
                AdditionalFrameData = new byte[data.Length - LENGTH];
                Buffer.BlockCopy(data, LENGTH, AdditionalFrameData, 0, AdditionalFrameData.Length);
            }
            else
                AdditionalFrameData = null;

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

            data[0U] = FrameType;                                               // Frame Type
            for (int i = 0; i < IMBE_BUF_LEN; i++)
                data[i + 1U] = IMBE[i];                                         // IMBE

            data[12U] = (byte)(((TotalErrors & 0x07U) << 5) +                   // Total Errors
                (MuteFrame ? 0x02U : 0x00U) +                                   // Mute Frame Flag
                (LostFrame ? 0x01U : 0x00U));                                   // Lost Frame Flag
            data[13U] = (byte)(((SuperFrameCnt & 0x03U) << 2) +                 // Superframe Count
                (Busy & 0x03U));                                                // Busy Status

            if (AdditionalFrameData != null)
            {
                if (data.Length >= 14U + AdditionalFrameData.Length)
                    Buffer.BlockCopy(AdditionalFrameData, 0, data, 14, AdditionalFrameData.Length);
            }
        }
    } // public class FullRateVoice
} // namespace dvmdfsi.DFSI.RTP
