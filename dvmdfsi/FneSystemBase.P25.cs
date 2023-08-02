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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Serilog;

using dvmdfsi.FNE;
using dvmdfsi.FNE.P25;

namespace dvmdfsi
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase
    {
        private const int P25_MSG_HDR_SIZE = 24;
        private const int IMBE_BUF_LEN = 11;

        private byte[] netLDU1;
        private byte[] netLDU2;

        /*
        ** Methods
        */

        /// <summary>
        /// Callback used to validate incoming P25 data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="duid">P25 DUID</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected virtual bool P25DataValidate(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId)
        {
            return true;
        }

        /// <summary>
        /// Event handler used to pre-process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void P25DataPreprocess(object sender, P25DataReceivedEvent e)
        {
            return;
        }

        /// <summary>
        /// Creates an P25 frame message header.
        /// </summary>
        /// <param name="duid"></param>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        private void CreateP25MessageHdr(byte duid, uint srcId, uint dstId, ref byte[] data)
        {
            FneUtils.StringToBytes(Constants.TAG_P25_DATA, data, 0, Constants.TAG_P25_DATA.Length);

            data[4U] = P25Defines.LC_GROUP;                                                 // LCO

            FneUtils.Write3Bytes(srcId, ref data, 5);                                       // Source Address
            FneUtils.Write3Bytes(dstId, ref data, 8);                                       // Destination Address

            data[11U] = 0;                                                                  // System ID
            data[12U] = 0;

            data[14U] = 0;                                                                  // Control Byte

            data[15U] = 0;                                                                  // MFId

            data[16U] = 0;                                                                  // Network ID
            data[17U] = 0;
            data[18U] = 0;

            data[20U] = 0;                                                                  // LSD 1
            data[21U] = 0;                                                                  // LSD 2

            data[22U] = duid;                                                               // DUID

            data[180U] = 0;                                                                 // Frame Type
        }

        /// <summary>
        /// Helper to send a P25 TDU message.
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <param name="grantDemand"></param>
        private void SendP25TDU(uint srcId, uint dstId, bool grantDemand = false)
        {
            FnePeer peer = (FnePeer)fne;
            ushort pktSeq = peer.pktSeq(true);

            byte[] payload = new byte[200];
            CreateP25MessageHdr((byte)P25DUID.TDU, srcId, dstId, ref payload);
            payload[23U] = P25_MSG_HDR_SIZE;

            // if this TDU is demanding a grant, set the grant demand control bit
            if (grantDemand)
                payload[14U] |= 0x80;

            peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), payload, pktSeq, txStreamId);
        }

        /// <summary>
        /// Encode a logical link data unit 1.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        /// <param name="imbe"></param>
        /// <param name="frameType"></param>
        private void EncodeLDU1(ref byte[] data, int offset, uint srcId, uint dstId, byte[] imbe, byte frameType)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (imbe == null)
                throw new ArgumentNullException("imbe");

            // determine the LDU1 DFSI frame length, its variable
            uint frameLength = P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES;
            switch (frameType)
            {
                case P25DFSI.P25_DFSI_LDU1_VOICE1:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE2:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE3:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE4:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE4_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE5:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE5_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE6:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE6_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE7:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE7_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE8:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE8_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE9:
                    frameLength = P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES;
                    break;
                default:
                    return;
            }

            byte[] dfsiFrame = new byte[frameLength];

            dfsiFrame[0U] = frameType;                                                      // Frame Type

            // different frame types mean different things
            switch (frameType)
            {
                case P25DFSI.P25_DFSI_LDU1_VOICE2:
                    {
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 1, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[12U] = P25DFSI.P25_DFSI_DEF_SOURCE;                       // Source
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE3:
                    {
                        dfsiFrame[1U] = P25Defines.LC_GROUP;                                // LCO
                        dfsiFrame[2U] = 0;                                                  // MFId
                        dfsiFrame[3U] = 0;                                                  // Service Options
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE4:
                    {
                        dfsiFrame[1U] = (byte)((dstId >> 16) & 0xFFU);                      // Talkgroup Address
                        dfsiFrame[2U] = (byte)((dstId >> 8) & 0xFFU);
                        dfsiFrame[3U] = (byte)((dstId >> 0) & 0xFFU);
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE5:
                    {
                        dfsiFrame[1U] = (byte)((srcId >> 16) & 0xFFU);                      // Source Address
                        dfsiFrame[2U] = (byte)((srcId >> 8) & 0xFFU);
                        dfsiFrame[3U] = (byte)((srcId >> 0) & 0xFFU);
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE6:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE7:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE8:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE9:
                    {
                        dfsiFrame[1U] = 0;                                                  // LSD MSB
                        dfsiFrame[2U] = 0;                                                  // LSD LSB
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 4, IMBE_BUF_LEN);              // IMBE
                    }
                    break;

                case P25DFSI.P25_DFSI_LDU1_VOICE1:
                default:
                    {
                        dfsiFrame[1U] = 0x02;                                               //
                        dfsiFrame[2U] = P25DFSI.P25_DFSI_RT_ENABLED;                        // RT/RT Mode Flag
                        dfsiFrame[3U] = P25DFSI.P25_DFSI_START_FLAG;                        // Start/Stop Flag
                        dfsiFrame[4U] = P25DFSI.P25_DFSI_TYPE_VOICE;                        // Type Flag
                        dfsiFrame[5U] = P25DFSI.P25_DFSI_DEF_ICW_SOURCE;                    // ICW Flag
                        dfsiFrame[6U] = 0;                                                  // RSSI
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 10, IMBE_BUF_LEN);             // IMBE
                        dfsiFrame[21U] = P25DFSI.P25_DFSI_DEF_SOURCE;                       // Source
                    }
                    break;
            }

            Buffer.BlockCopy(dfsiFrame, 0, data, offset, (int)frameLength);
        }

        /// <summary>
        /// Creates an P25 LDU1 frame message.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        private void CreateP25LDU1Message(ref byte[] data, uint srcId, uint dstId)
        {
            // pack DFSI data
            int count = P25_MSG_HDR_SIZE;
            byte[] imbe = new byte[IMBE_BUF_LEN];

            Buffer.BlockCopy(netLDU1, 10, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 24, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE1);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 26, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 46, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE2);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 55, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 60, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE3);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 80, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 77, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE4);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE4_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 105, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 94, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE5);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE5_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 130, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 111, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE6);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE6_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 155, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 128, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE7);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE7_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 180, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 145, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE8);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE8_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 204, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 162, srcId, dstId, imbe, P25DFSI.P25_DFSI_LDU1_VOICE9);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES;

            data[23U] = (byte)count;
        }

        /// <summary>
        /// Encode a logical link data unit 2.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="imbe"></param>
        /// <param name="frameType"></param>
        private void EncodeLDU2(ref byte[] data, int offset, byte[] imbe, byte frameType)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (imbe == null)
                throw new ArgumentNullException("imbe");

            // determine the LDU2 DFSI frame length, its variable
            uint frameLength = P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES;
            switch (frameType)
            {
                case P25DFSI.P25_DFSI_LDU2_VOICE10:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE11:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE12:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE13:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE13_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE14:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE14_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE15:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE15_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE16:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE16_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE17:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE17_FRAME_LENGTH_BYTES;
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE18:
                    frameLength = P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES;
                    break;
                default:
                    return;
            }

            byte[] dfsiFrame = new byte[frameLength];

            dfsiFrame[0U] = frameType;                                                      // Frame Type

            // different frame types mean different things
            switch (frameType)
            {
                case P25DFSI.P25_DFSI_LDU2_VOICE11:
                    {
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 1, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[12U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE12:
                    {
                        dfsiFrame[1U] = 0;                                                  // Message Indicator
                        dfsiFrame[2U] = 0;
                        dfsiFrame[3U] = 0;
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE13:
                    {
                        dfsiFrame[1U] = 0;                                                  // Message Indicator
                        dfsiFrame[2U] = 0;
                        dfsiFrame[3U] = 0;
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE14:
                    {
                        dfsiFrame[1U] = 0;                                                  // Message Indicator
                        dfsiFrame[2U] = 0;
                        dfsiFrame[3U] = 0;
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE15:
                    {
                        dfsiFrame[1U] = 0x80;                                               // Algorithm ID
                        dfsiFrame[2U] = 0;                                                  // Key ID
                        dfsiFrame[3U] = 0;
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE16:
                    {
                        // first 3 bytes of frame are supposed to be
                        // part of the RS(24, 16, 9) of the VOICE12, 13, 14, 15
                        // control bytes
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE17:
                    {
                        // first 3 bytes of frame are supposed to be
                        // part of the RS(24, 16, 9) of the VOICE12, 13, 14, 15
                        // control bytes
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                        dfsiFrame[16U] = P25DFSI.P25_DFSI_STATUS_ERASE;                     // Status
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE18:
                    {
                        dfsiFrame[1U] = 0;                                                  // LSD MSB
                        dfsiFrame[2U] = 0;                                                  // LSD LSB
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 4, IMBE_BUF_LEN);              // IMBE
                    }
                    break;

                case P25DFSI.P25_DFSI_LDU2_VOICE10:
                default:
                    {
                        dfsiFrame[1U] = 0x02;                                               //
                        dfsiFrame[2U] = P25DFSI.P25_DFSI_RT_ENABLED;                        // RT/RT Mode Flag
                        dfsiFrame[3U] = P25DFSI.P25_DFSI_START_FLAG;                        // Start/Stop Flag
                        dfsiFrame[4U] = P25DFSI.P25_DFSI_TYPE_VOICE;                        // Type Flag
                        dfsiFrame[5U] = P25DFSI.P25_DFSI_DEF_ICW_SOURCE;                    // ICW Flag
                        dfsiFrame[6U] = 0;                                                  // RSSI
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 10, IMBE_BUF_LEN);             // IMBE
                        dfsiFrame[21U] = P25DFSI.P25_DFSI_DEF_SOURCE;                       // Source
                    }
                    break;
            }

            Buffer.BlockCopy(dfsiFrame, 0, data, offset, (int)frameLength);
        }

        /// <summary>
        /// Creates an P25 LDU2 frame message.
        /// </summary>
        /// <param name="data"></param>
        private void CreateP25LDU2Message(ref byte[] data)
        {
            // pack DFSI data
            int count = P25_MSG_HDR_SIZE;
            byte[] imbe = new byte[IMBE_BUF_LEN];

            Buffer.BlockCopy(netLDU2, 10, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 24, imbe, P25DFSI.P25_DFSI_LDU2_VOICE10);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 26, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 46, imbe, P25DFSI.P25_DFSI_LDU2_VOICE11);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 55, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 60, imbe, P25DFSI.P25_DFSI_LDU2_VOICE12);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 80, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 77, imbe, P25DFSI.P25_DFSI_LDU2_VOICE13);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE13_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 105, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 94, imbe, P25DFSI.P25_DFSI_LDU2_VOICE14);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE14_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 130, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 111, imbe, P25DFSI.P25_DFSI_LDU2_VOICE15);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE15_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 155, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 128, imbe, P25DFSI.P25_DFSI_LDU2_VOICE16);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE16_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 180, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 145, imbe, P25DFSI.P25_DFSI_LDU2_VOICE17);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE17_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 204, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 162, imbe, P25DFSI.P25_DFSI_LDU2_VOICE18);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES;

            data[23U] = (byte)count;
        }

        /// <summary>
        /// Helper to decode and playback P25 IMBE frames as PCM audio.
        /// </summary>
        /// <param name="ldu"></param>
        /// <param name="e"></param>
        private void P25SendFrame(uint sysId, uint netId, byte[] ldu, P25DataReceivedEvent e)
        {
            // decode 9 IMBE codewords into PCM samples
            for (int n = 0; n < 9; n++)
            {
                byte[] imbe = new byte[IMBE_BUF_LEN];
                switch (n)
                {
                    case 0:
                        Buffer.BlockCopy(ldu, 10, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 1:
                        Buffer.BlockCopy(ldu, 26, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 2:
                        Buffer.BlockCopy(ldu, 55, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 3:
                        Buffer.BlockCopy(ldu, 80, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 4:
                        Buffer.BlockCopy(ldu, 105, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 5:
                        Buffer.BlockCopy(ldu, 130, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 6:
                        Buffer.BlockCopy(ldu, 155, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 7:
                        Buffer.BlockCopy(ldu, 180, imbe, 0, IMBE_BUF_LEN);
                        break;
                    case 8:
                        Buffer.BlockCopy(ldu, 204, imbe, 0, IMBE_BUF_LEN);
                        break;
                }

                // TODO TODO TODO
            }
        }

        /// <summary>
        /// Event handler used to process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void P25DataReceived(object sender, P25DataReceivedEvent e)
        {
            DateTime pktTime = DateTime.Now;

            if (e.DUID == P25DUID.HDU || e.DUID == P25DUID.TSDU || e.DUID == P25DUID.PDU)
                return;

            uint sysId = (uint)((e.Data[11U] << 8) | (e.Data[12U] << 0));
            uint netId = FneUtils.Bytes3ToUInt32(e.Data, 16);

            byte len = e.Data[23];
            byte[] data = new byte[len];
            for (int i = 24; i < len; i++)
                data[i - 24] = e.Data[i];

            if (e.CallType == CallType.GROUP)
            {
                if (e.SrcId == 0)
                {
                    Log.Logger.Warning($"({SystemName}) P25D: Received call from SRC_ID {e.SrcId}? Dropping call e.Data.");
                    return;
                }

                // is this a new call stream?
                if (e.StreamId != status[P25_FIXED_SLOT].RxStreamId && ((e.DUID != P25DUID.TDU) && (e.DUID != P25DUID.TDULC)))
                {
                    status[P25_FIXED_SLOT].RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) P25D: Traffic *CALL START     * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");
                    // TODO TODO TODO
                }

                if (((e.DUID == P25DUID.TDU) || (e.DUID == P25DUID.TDULC)) && (status[P25_FIXED_SLOT].RxType != FrameType.TERMINATOR))
                {
                    TimeSpan callDuration = pktTime - status[P25_FIXED_SLOT].RxStart;
                    Log.Logger.Information($"({SystemName}) P25D: Traffic *CALL END       * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration} [STREAM ID {e.StreamId}]");
                }


                int count = 0;
                switch (e.DUID)
                {
                    case P25DUID.LDU1:
                        {
                            // The '62', '63', '64', '65', '66', '67', '68', '69', '6A' records are LDU1
                            if ((data[0U] == 0x62U) && (data[22U] == 0x63U) &&
                                (data[36U] == 0x64U) && (data[53U] == 0x65U) &&
                                (data[70U] == 0x66U) && (data[87U] == 0x67U) &&
                                (data[104U] == 0x68U) && (data[121U] == 0x69U) &&
                                (data[138U] == 0x6AU))
                            {
                                // The '62' record - IMBE Voice 1
                                Buffer.BlockCopy(data, count, netLDU1, 0, 22);
                                count += 22;

                                // The '63' record - IMBE Voice 2
                                Buffer.BlockCopy(data, count, netLDU1, 25, 14);
                                count += 14;

                                // The '64' record - IMBE Voice 3 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 50, 17);
                                count += 17;

                                // The '65' record - IMBE Voice 4 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 75, 17);
                                count += 17;

                                // The '66' record - IMBE Voice 5 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 100, 17);
                                count += 17;

                                // The '67' record - IMBE Voice 6 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 125, 17);
                                count += 17;

                                // The '68' record - IMBE Voice 7 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 150, 17);
                                count += 17;

                                // The '69' record - IMBE Voice 8 + Link Control
                                Buffer.BlockCopy(data, count, netLDU1, 175, 17);
                                count += 17;

                                // The '6A' record - IMBE Voice 9 + Low Speed Data
                                Buffer.BlockCopy(data, count, netLDU1, 200, 16);
                                count += 16;

                                // decode 9 IMBE codewords into PCM samples
                                P25SendFrame(sysId, netId, netLDU1, e);
                            }
                        }
                        break;
                    case P25DUID.LDU2:
                        {
                            // The '6B', '6C', '6D', '6E', '6F', '70', '71', '72', '73' records are LDU2
                            if ((data[0U] == 0x6BU) && (data[22U] == 0x6CU) &&
                                (data[36U] == 0x6DU) && (data[53U] == 0x6EU) &&
                                (data[70U] == 0x6FU) && (data[87U] == 0x70U) &&
                                (data[104U] == 0x71U) && (data[121U] == 0x72U) &&
                                (data[138U] == 0x73U))
                            {
                                // The '6B' record - IMBE Voice 10
                                Buffer.BlockCopy(data, count, netLDU2, 0, 22);
                                count += 22;

                                // The '6C' record - IMBE Voice 11
                                Buffer.BlockCopy(data, count, netLDU2, 25, 14);
                                count += 14;

                                // The '6D' record - IMBE Voice 12 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 50, 17);
                                count += 17;

                                // The '6E' record - IMBE Voice 13 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 75, 17);
                                count += 17;

                                // The '6F' record - IMBE Voice 14 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 100, 17);
                                count += 17;

                                // The '70' record - IMBE Voice 15 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 125, 17);
                                count += 17;

                                // The '71' record - IMBE Voice 16 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 150, 17);
                                count += 17;

                                // The '72' record - IMBE Voice 17 + Encryption Sync
                                Buffer.BlockCopy(data, count, netLDU2, 175, 17);
                                count += 17;

                                // The '73' record - IMBE Voice 18 + Low Speed Data
                                Buffer.BlockCopy(data, count, netLDU2, 200, 16);
                                count += 16;

                                // decode 9 IMBE codewords into PCM samples
                                P25SendFrame(sysId, netId, netLDU2, e);
                            }
                        }
                        break;
                }

                status[P25_FIXED_SLOT].RxRFS = e.SrcId;
                status[P25_FIXED_SLOT].RxType = e.FrameType;
                status[P25_FIXED_SLOT].RxTGId = e.DstId;
                status[P25_FIXED_SLOT].RxTime = pktTime;
                status[P25_FIXED_SLOT].RxStreamId = e.StreamId;
            }
            else
                Log.Logger.Warning($"({SystemName}) P25D: DFSI does not support private calls.");

            return;
        }
    } // public abstract partial class FneSystemBase
} // namespace dvmdfsi
