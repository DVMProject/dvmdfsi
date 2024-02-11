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
using System.Collections.Generic;

using Serilog;

using fnecore.P25;
using dvmdfsi.DFSI.RTP;

namespace dvmdfsi.DFSI
{
    /// <summary>
    /// This class implements a P25 payload which encapsulates one or more P25 block
    /// objects.
    /// </summary>
    public class P25RTPPayload
    {
        /// <summary>
        /// Control Block.
        /// </summary>
        public ControlOctet Control
        {
            get;
            set;
        }

        /// <summary>
        /// Block Headers.
        /// </summary>
        public List<BlockHeader> BlockHeaders
        {
            get;
            set;
        }

        /// <summary>
        /// Start of Stream.
        /// </summary>
        public StartOfStream StartOfStream
        {
            get;
            set;
        }

        /// <summary>
        /// Full-rate ISSI Voice Blocks.
        /// </summary>
        public List<FullRateVoice> FullRateVoiceBlocks
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<int, int> BlockHeaderToVoiceBlock
        {
            get;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="P25RTPPayload"/> class.
        /// </summary>
        public P25RTPPayload()
        {
            Control = new ControlOctet();
            Control.Signal = false;
            BlockHeaders = new List<BlockHeader>();
            StartOfStream = null;
            FullRateVoiceBlocks = new List<FullRateVoice>();
            BlockHeaderToVoiceBlock = new Dictionary<int, int>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="P25RTPPayload"/> class.
        /// </summary>
        public P25RTPPayload(byte[] data) : this()
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

            int offs = 1;

            // decode control block
            Control = new ControlOctet(data[0]);

            // decode block headers
            int blockHeaderCount = Control.BlockHeaderCount;
            for (int i = 0; i < blockHeaderCount; i++)
            {
                byte[] buffer = new byte[BlockHeader.LENGTH];
                Buffer.BlockCopy(data, offs, buffer, 0, BlockHeader.LENGTH);
                BlockHeader header = new BlockHeader(buffer);
                BlockHeaders.Add(header);

                offs += BlockHeader.LENGTH;
            }

            // decode voice blocks
            for (int i = 0; i < blockHeaderCount; i++)
            {
                BlockHeader header = BlockHeaders[i];
                switch (header.Type)
                {
                    case BlockType.START_OF_STREAM:
                        {
                            byte[] buffer = new byte[StartOfStream.LENGTH];
                            Buffer.BlockCopy(data, offs, buffer, 0, StartOfStream.LENGTH);
                            StartOfStream = new StartOfStream(buffer);
                            offs += StartOfStream.LENGTH;
                        }
                        break;

                    case BlockType.FULL_RATE_VOICE:
                        {
                            byte frameType = data[offs];
                            byte[] buffer = null;
                            switch (frameType)
                            {
                                case P25DFSI.P25_DFSI_LDU1_VOICE1:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE2:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE3:
                                case P25DFSI.P25_DFSI_LDU1_VOICE4:
                                case P25DFSI.P25_DFSI_LDU1_VOICE5:
                                case P25DFSI.P25_DFSI_LDU1_VOICE6:
                                case P25DFSI.P25_DFSI_LDU1_VOICE7:
                                case P25DFSI.P25_DFSI_LDU1_VOICE8:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE9:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;

                                case P25DFSI.P25_DFSI_LDU2_VOICE10:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE11:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE12:
                                case P25DFSI.P25_DFSI_LDU2_VOICE13:
                                case P25DFSI.P25_DFSI_LDU2_VOICE14:
                                case P25DFSI.P25_DFSI_LDU2_VOICE15:
                                case P25DFSI.P25_DFSI_LDU2_VOICE16:
                                case P25DFSI.P25_DFSI_LDU2_VOICE17:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE18:
                                    {
                                        buffer = new byte[P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES];
                                        int lenToCopy = (data.Length >= P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES) ? (int)P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES : data.Length;
                                        Buffer.BlockCopy(data, offs, buffer, 0, lenToCopy);

                                        FullRateVoice voice = new FullRateVoice(buffer);
                                        FullRateVoiceBlocks.Add(voice);
                                        BlockHeaderToVoiceBlock.Add(i, FullRateVoiceBlocks.Count - 1);
                                        offs += lenToCopy;
                                    }
                                    break;
                            }
                        }
                        break;

                    default:
                        Log.Logger.Error($"Unknown/Unhandled DFSI opcode {header.Type}");
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate block size.
        /// </summary>
        public int CalculateSize()
        {
            int totalLength = 0;

            int blockHeaderCount = Control.BlockHeaderCount;
            if (BlockHeaders.Count - 1 != blockHeaderCount)
            {
                Log.Logger.Error($"Number of block headers in control octect do not match number of block headers in P25 payload. {BlockHeaders.Count - 1} != {blockHeaderCount}");
                return -1;
            }

            // ensure we have block headers
            if (BlockHeaders.Count == 0)
            {
                Log.Logger.Error($"P25 packet incomplete. No block headers.");
                return -1;
            }

            // encode control octet
            totalLength += ControlOctet.LENGTH;

            // encode block headers
            foreach (BlockHeader header in BlockHeaders)
                totalLength += BlockHeader.LENGTH;

            // encode start of stream control block
            if (StartOfStream != null)
                totalLength += StartOfStream.LENGTH;

            // encode voice frames
            if (FullRateVoiceBlocks.Count > 0)
            {
                foreach (FullRateVoice voice in FullRateVoiceBlocks)
                    totalLength += voice.Size();
            }

            return totalLength;
        }

        /// <summary>
        /// Encode a block.
        /// </summary>
        /// <param name="data"></param>
        public void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            int offs = 0;

            int blockHeaderCount = Control.BlockHeaderCount;
            if (BlockHeaders.Count - 1 != blockHeaderCount)
            {
                Log.Logger.Error($"Number of block headers in control octect do not match number of block headers in P25 payload. {BlockHeaders.Count - 1} != {blockHeaderCount}");
                return;
            }

            // ensure we have block headers
            if (BlockHeaders.Count == 0)
            {
                Log.Logger.Error($"P25 packet incomplete. No block headers.");
                return;
            }

            byte[] buffer = null;

            // encode control octet
            byte controlByte = 0;
            Control.Encode(ref controlByte);
            data[0] = controlByte;
            offs += ControlOctet.LENGTH;

            // encode block headers
            uint blockBufLen = (uint)(blockHeaderCount * BlockHeader.LENGTH);
            buffer = new byte[blockBufLen];
            int blockOffs = 0;
            foreach (BlockHeader header in BlockHeaders)
            {
                byte[] blockBuf = new byte[BlockHeader.LENGTH];
                header.Encode(ref blockBuf);
                Buffer.BlockCopy(blockBuf, 0, buffer, blockOffs, BlockHeader.LENGTH);
                blockOffs += BlockHeader.LENGTH;
            }

            Buffer.BlockCopy(buffer, 0, data, offs, buffer.Length);
            offs += buffer.Length;

            // encode PTT control block
            if (StartOfStream != null)
            {
                buffer = new byte[StartOfStream.LENGTH];
                StartOfStream.Encode(ref buffer);
                Buffer.BlockCopy(buffer, 0, data, offs, StartOfStream.LENGTH);
                offs += StartOfStream.LENGTH;
            }

            // encode voice frames
            if (FullRateVoiceBlocks.Count > 0)
            {
                foreach (FullRateVoice voice in FullRateVoiceBlocks)
                {
                    byte[] voiceBuf = new byte[voice.Size()];
                    voice.Encode(ref voiceBuf);
                    Buffer.BlockCopy(voiceBuf, 0, data, offs, voice.Size());
                    offs += voice.Size();
                }
            }
        }
    } // public class P25RTPPayload
} // namespace dvmdfsi.DFSI
