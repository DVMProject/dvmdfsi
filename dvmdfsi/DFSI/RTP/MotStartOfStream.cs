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

using fnecore;
using fnecore.P25;

namespace dvmdfsi.DFSI.RTP
{
    /// <summary>
    /// 
    /// </summary>
    public enum RTFlag : byte
    {
        ENABLED = 0x02,
        DISABLED = 0x04
    } // public enum RTFlag : byte

    /// <summary>
    /// 
    /// </summary>
    public enum StartStopFlag : byte
    {
        START = 0x0C,
        STOP = 0x25
    } // public enum StartStopFlag : byte

    /// <summary>
    /// 
    /// </summary>
    public enum StreamTypeFlag : byte
    {
        VOICE = 0x0B
    } // public enum StreamTypeFlag : byte

    /// <summary>
    /// Implements a P25 Motorola start of stream packet.
    /// </summary>
    /// 
    /// Byte 0               1               2               3
    /// Bit  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Fixed Mark  |  RT Mode Flag |  Start/Stop   |  Type Flag    |
    ///     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |   Reserved                                                    |
    ///     +               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///     |               |
    ///     +-+-+-+-+-+-+-+-+
    public class MotStartOfStream
    {
        public const int LENGTH = 10;
        private const byte FIXED_MARKER = 0x02;

        /// <summary>
        /// 
        /// </summary>
        public byte Marker { get => FIXED_MARKER; }

        /// <summary>
        /// 
        /// </summary>
        public RTFlag RT
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public StartStopFlag StartStop
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public StreamTypeFlag Type
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MotStartOfStream"/> class.
        /// </summary>
        public MotStartOfStream()
        {
            RT = RTFlag.DISABLED;
            StartStop = StartStopFlag.START;
            Type = StreamTypeFlag.VOICE;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MotStartOfStream"/> class.
        /// </summary>
        /// <param name="data"></param>
        public MotStartOfStream(byte[] data)
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

            // offset 0 is the frame type
            // offset 1 is the fixed marker
            RT = (RTFlag)data[2U];                                              // RT Mode Flag
            StartStop = (StartStopFlag)data[3U];                                // Start/Stop Flag
            Type = (StreamTypeFlag)data[4U];                                    // Stream Type

            return true;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="skipFrameType"></param>
        public void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            data[0U] = P25DFSI.P25_DFSI_MOT_START_STOP;                         // Frame Type
            data[1U] = FIXED_MARKER;                                            // Fixed Frame Marker
            data[2U] = (byte)RT;                                                // RT Mode Flag
            data[3U] = (byte)StartStop;                                         // Start/Stop Flag
            data[4U] = (byte)Type;                                              // Stream Type
        }
    } // public class MotStartOfStream
} // namespace dvmdfsi.DFSI.RTP
