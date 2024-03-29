﻿// SPDX-License-Identifier: AGPL-3.0-only
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
using Serilog;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// 
    /// </summary>
    public enum RSSIValidityFlag : byte
    {
        INVALID = 0x00,
        VALID = 0x1A
    } // public enum RSSIValidityFlag : byte

    /// <summary>
    /// Implements a P25 Motorola voice frame 1/10 start.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Encoded Motorola Start of Stream                            |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   ICW Flag ?  |     RSSI      |  RSSI Valid   |     RSSI      |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Adj MM ?    |    Full Rate Voice Frame                      |
    ///     +-+-+-+-+-+-+-+-+                                               +
    ///     |                                                               |
    ///     +                                                               +
    ///     |                                                               |
    ///     +               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //      |               |
    ///     +=+=+=+=+=+=+=+=+
    public class MotStartVoiceFrame
    {
        public const int LENGTH = 22;

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
        public byte AdjMM
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
        public MotFullRateVoice FullRateVoice
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MotStartVoiceFrame"/> class.
        /// </summary>
        public MotStartVoiceFrame()
        {
            ICW = 0;
            RSSI = 0;
            RSSIValidity = RSSIValidityFlag.INVALID;
            AdjMM = 0;

            StartOfStream = null;
            FullRateVoice = null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotStartVoiceFrame"/> class.
        /// </summary>
        /// <param name="data"></param>
        public MotStartVoiceFrame(byte[] data)
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

            Log.Logger.Debug($"({Program.Configuration.Name}) decoding Mot voice start frame {BitConverter.ToString(data).Replace("-"," ")}");

            // decode start of stream data
            StartOfStream = new MotStartOfStream();
            {
                byte[] buffer = new byte[MotStartOfStream.LENGTH];
                Buffer.BlockCopy(data, 1, buffer, 0, 4);
                StartOfStream.Decode(buffer);
            }

            // decode the full rate voice data
            FullRateVoice = new MotFullRateVoice();
            {
                byte[] buffer = new byte[MotFullRateVoice.SHORTENED_LENGTH];
                buffer[0U] = data[0U];
                Buffer.BlockCopy(data, 10, buffer, 1, MotFullRateVoice.SHORTENED_LENGTH - 2);
                FullRateVoice.Decode(buffer, true);
            }

            ICW = data[5U];                                                     // ICW Flag ?
            RSSI = data[6U];                                                    // RSSI
            RSSIValidity = (RSSIValidityFlag)data[7U];                          // RSSI Validity
            AdjMM = data[9U];                                                   // Adj MM ?

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
            if (FullRateVoice == null)
                return;

            // encode start of stream data
            if (StartOfStream != null)
            {
                byte[] buffer = new byte[MotStartOfStream.LENGTH];
                StartOfStream.Encode(ref buffer);
                Buffer.BlockCopy(buffer, 1, data, 1, MotStartOfStream.LENGTH - 1);
            }

            // encode full rate voice data
            if (FullRateVoice != null)
            {
                byte[] buffer = new byte[MotFullRateVoice.SHORTENED_LENGTH];
                FullRateVoice.Encode(ref buffer, true);
                data[0U] = FullRateVoice.FrameType;
                Buffer.BlockCopy(buffer, 1, data, 10, MotFullRateVoice.SHORTENED_LENGTH - 1);
            }

            data[5U] = ICW;                                                     // ICW Flag ?
            data[6U] = RSSI;                                                    // RSSI
            data[7U] = (byte)RSSIValidity;                                      // RSSI Validity
            data[8U] = RSSI;                                                    // RSSI
            data[9U] = AdjMM;                                                   // Adj MM ?
        }
    } // public class MotStartOfStream
} // namespace dvmdfsi.DFSI.RTP
