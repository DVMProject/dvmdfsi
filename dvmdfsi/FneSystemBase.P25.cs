// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2023-2024 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2023 Patrick McDonnell, W3AXL
*
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Serilog;

using fnecore;
using fnecore.EDAC.RS;
using fnecore.P25;

using dvmdfsi.DFSI;
using dvmdfsi.DFSI.RTP;

namespace dvmdfsi
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {
        public DateTime RxStart = DateTime.Now;
        public uint RxStreamId = 0;
        public FrameType RxType = FrameType.TERMINATOR;

        private static DateTime start = DateTime.Now;
        private const int IMBE_BUF_LEN = 11;

        private byte[] netLDU1;
        private byte[] netLDU2;

        private uint p25SeqNo = 0;
        private byte p25N = 0;

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
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected override bool P25DataValidate(uint peerId, uint srcId, uint dstId, CallType callType, P25DUID duid, FrameType frameType, uint streamId, byte[] message)
        {
            return true;
        }

        /// <summary>
        /// Event handler used to pre-process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void P25DataPreprocess(object sender, P25DataReceivedEvent e)
        {
            return;
        }

        /// <summary>
        /// Encode a logical link data unit 1.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="callData"></param>
        /// <param name="imbe"></param>
        /// <param name="frameType"></param>
        private void EncodeLDU1(ref byte[] data, int offset, RemoteCallData callData, byte[] imbe, byte frameType)
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
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE3:
                    {
                        dfsiFrame[1U] = callData.LCO;                                       // LCO
                        dfsiFrame[2U] = callData.MFId;                                      // MFId
                        dfsiFrame[3U] = callData.ServiceOptions;                            // Service Options
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE4:
                    {
                        dfsiFrame[1U] = (byte)((callData.DstId >> 16) & 0xFFU);             // Talkgroup Address
                        dfsiFrame[2U] = (byte)((callData.DstId >> 8) & 0xFFU);
                        dfsiFrame[3U] = (byte)((callData.DstId >> 0) & 0xFFU);
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE5:
                    {
                        dfsiFrame[1U] = (byte)((callData.SrcId >> 16) & 0xFFU);             // Source Address
                        dfsiFrame[2U] = (byte)((callData.SrcId >> 8) & 0xFFU);
                        dfsiFrame[3U] = (byte)((callData.SrcId >> 0) & 0xFFU);
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE6:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE7:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE8:
                    {
                        dfsiFrame[1U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[2U] = 0;                                                  // RS (24,12,13)
                        dfsiFrame[3U] = 0;                                                  // RS (24,12,13)
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU1_VOICE9:
                    {
                        dfsiFrame[1U] = callData.LSD1;                                      // LSD MSB
                        dfsiFrame[2U] = callData.LSD2;                                      // LSD LSB
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 4, IMBE_BUF_LEN);              // IMBE
                    }
                    break;

                case P25DFSI.P25_DFSI_LDU1_VOICE1:
                default:
                    {
                        dfsiFrame[6U] = 0;                                                  // RSSI
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 10, IMBE_BUF_LEN);             // IMBE
                    }
                    break;
            }

            Buffer.BlockCopy(dfsiFrame, 0, data, offset, (int)frameLength);
        }

        /// <summary>
        /// Creates an P25 LDU1 frame message.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callData"></param>
        private void CreateP25LDU1Message(ref byte[] data, RemoteCallData callData)
        {
            // pack DFSI data
            int count = P25_MSG_HDR_SIZE;
            byte[] imbe = new byte[IMBE_BUF_LEN];

            Buffer.BlockCopy(netLDU1, 10, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 24, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE1);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 26, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 46, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE2);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 55, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 60, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE3);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 80, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 77, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE4);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE4_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 105, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 94, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE5);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE5_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 130, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 111, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE6);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE6_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 155, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 128, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE7);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE7_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 180, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 145, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE8);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE8_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU1, 204, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU1(ref data, 162, callData, imbe, P25DFSI.P25_DFSI_LDU1_VOICE9);
            count += (int)P25DFSI.P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES;

            data[23U] = (byte)count;
        }

        /// <summary>
        /// Encode a logical link data unit 2.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="callData"></param>
        /// <param name="imbe"></param>
        /// <param name="frameType"></param>
        private void EncodeLDU2(ref byte[] data, int offset, RemoteCallData callData, byte[] imbe, byte frameType)
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
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE12:
                    {
                        dfsiFrame[1U] = callData.MessageIndicator[0];                       // Message Indicator
                        dfsiFrame[2U] = callData.MessageIndicator[1];
                        dfsiFrame[3U] = callData.MessageIndicator[2];
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE13:
                    {
                        dfsiFrame[1U] = callData.MessageIndicator[3];                       // Message Indicator
                        dfsiFrame[2U] = callData.MessageIndicator[4];
                        dfsiFrame[3U] = callData.MessageIndicator[5];
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE14:
                    {
                        dfsiFrame[1U] = callData.MessageIndicator[6];                       // Message Indicator
                        dfsiFrame[2U] = callData.MessageIndicator[7];
                        dfsiFrame[3U] = callData.MessageIndicator[8];
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE15:
                    {
                        dfsiFrame[1U] = callData.AlgorithmId;                               // Algorithm ID
                        dfsiFrame[2U] = (byte)((callData.KeyId >> 8) & 0xFFU);              // Key ID
                        dfsiFrame[3U] = (byte)((callData.KeyId >> 0) & 0xFFU);
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE16:
                    {
                        // first 3 bytes of frame are supposed to be
                        // part of the RS(24, 16, 9) of the VOICE12, 13, 14, 15
                        // control bytes
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE17:
                    {
                        // first 3 bytes of frame are supposed to be
                        // part of the RS(24, 16, 9) of the VOICE12, 13, 14, 15
                        // control bytes
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 5, IMBE_BUF_LEN);              // IMBE
                    }
                    break;
                case P25DFSI.P25_DFSI_LDU2_VOICE18:
                    {
                        dfsiFrame[1U] = callData.LSD1;                                      // LSD MSB
                        dfsiFrame[2U] = callData.LSD2;                                      // LSD LSB
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 4, IMBE_BUF_LEN);              // IMBE
                    }
                    break;

                case P25DFSI.P25_DFSI_LDU2_VOICE10:
                default:
                    {
                        dfsiFrame[6U] = 0;                                                  // RSSI
                        Buffer.BlockCopy(imbe, 0, dfsiFrame, 10, IMBE_BUF_LEN);             // IMBE
                    }
                    break;
            }

            Buffer.BlockCopy(dfsiFrame, 0, data, offset, (int)frameLength);
        }

        /// <summary>
        /// Creates an P25 LDU2 frame message.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callData"></param>
        private void CreateP25LDU2Message(ref byte[] data, RemoteCallData callData)
        {
            // pack DFSI data
            int count = P25_MSG_HDR_SIZE;
            byte[] imbe = new byte[IMBE_BUF_LEN];

            Buffer.BlockCopy(netLDU2, 10, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 24, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE10);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 26, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 46, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE11);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 55, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 60, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE12);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 80, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 77, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE13);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE13_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 105, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 94, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE14);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE14_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 130, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 111, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE15);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE15_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 155, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 128, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE16);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE16_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 180, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 145, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE17);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE17_FRAME_LENGTH_BYTES;

            Buffer.BlockCopy(netLDU2, 204, imbe, 0, IMBE_BUF_LEN);
            EncodeLDU2(ref data, 162, callData, imbe, P25DFSI.P25_DFSI_LDU2_VOICE18);
            count += (int)P25DFSI.P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES;

            data[23U] = (byte)count;
        }

        /// <summary>
        /// Helper to send DFSI start of stream.
        /// </summary>
        /// <remarks>This implements "the" manufacturer standard DFSI RTP frame handling.</remarks>
        /// <param name="e"></param>
        private void Mot_DFSIStartOfStream(P25DataReceivedEvent e)
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            MotStartOfStream start = new MotStartOfStream();
            start.StartStop = StartStopFlag.START;
            start.RT = RTFlag.ENABLED;    // TODO: make this selectable

            byte[] buffer = new byte[MotStartOfStream.LENGTH];
            start.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded mot p25 start frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);

            byte mfId = e.Data[15];
            byte algId = P25Defines.P25_ALGO_UNENCRYPT;
            ushort kid = 0x00;
            byte[] mi = new byte[P25Defines.P25_MI_LENGTH];

            // is this a LDU1 or LDU2?
            if (e.DUID == P25DUID.LDU1 || e.DUID == P25DUID.LDU2)
            {
                // check if this is the first frame of a call
                uint frameType = e.Data[180];
                if (frameType == P25Defines.P25_FT_HDU_VALID)
                {
                    algId = e.Data[181];
                    kid = FneUtils.ToUInt16(e.Data, 182);
                    Buffer.BlockCopy(e.Data, 184, mi, 0, P25Defines.P25_MI_LENGTH);
                }
            }

            // build header
            byte[] vhdr = new byte[P25DFSI.P25_DFSI_VHDR_LEN];
            Buffer.BlockCopy(mi, 0, vhdr, 0, P25Defines.P25_MI_LENGTH);

            vhdr[9] = e.Data[15];                                   // Manufacturer ID
            vhdr[10] = algId;                                       // Algorithm ID
            FneUtils.WriteBytes(kid, ref vhdr, 11);                 // Key ID
            FneUtils.WriteBytes((ushort)e.DstId, ref vhdr, 13);     // TGID

            vhdr = ReedSolomonAlgorithm.Encode(vhdr, ErrorCorrectionCodeType.ReedSolomon_362017);

            byte[] raw = new byte[P25DFSI.P25_DFSI_VHDR_RAW_LEN];
            uint offset = 0;
            for (int i = 0; i < raw.Length; i++, offset += 6)
                raw[i] = FneUtils.BIN2HEX(vhdr, offset);

            // VHDR1
            MotVoiceHeader1 vhdr1 = new MotVoiceHeader1();
            vhdr1.StartOfStream = new MotStartOfStream();
            vhdr1.StartOfStream.StartStop = StartStopFlag.START;
            vhdr1.StartOfStream.RT = RTFlag.ENABLED;    // TODO: make this selectable

            Buffer.BlockCopy(raw, 0, vhdr1.Header, 0, 8);
            Buffer.BlockCopy(raw, 8, vhdr1.Header, 9, 8);
            Buffer.BlockCopy(raw, 16, vhdr1.Header, 18, 2);

            buffer = new byte[MotVoiceHeader1.LENGTH];
            vhdr1.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded mot VHDR1 p25 frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);

            // VHDR2
            MotVoiceHeader2 vhdr2 = new MotVoiceHeader2();

            Buffer.BlockCopy(raw, 18, vhdr2.Header, 0, 8);
            Buffer.BlockCopy(raw, 26, vhdr2.Header, 9, 8);
            Buffer.BlockCopy(raw, 34, vhdr2.Header, 18, 2);

            buffer = new byte[MotVoiceHeader2.LENGTH];
            vhdr2.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded mot VHDR2 p25 frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);
        }

        /// <summary>
        /// Helper to send DFSI end of stream.
        /// </summary>
        /// <remarks>This implements "the" manufacturer standard DFSI RTP frame handling.</remarks>
        private void Mot_DFSIEndOfStream()
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            MotStartOfStream start = new MotStartOfStream();
            start.StartStop = StartStopFlag.STOP;

            byte[] buffer = new byte[MotStartOfStream.LENGTH];
            start.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded mot p25 end frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);
        }

        /// <summary>
        /// Helper to send DFSI start of stream.
        /// </summary>
        /// <remarks>This implements TIA-102.BAHA standard DFSI RTP frame handling.</remarks>
        /// <param name="e"></param>
        private void TIA_DFSIStartOfStream(P25DataReceivedEvent e)
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            P25RTPPayload payload = new P25RTPPayload();
            payload.Control.Signal = true;

            BlockHeader blockHeader = new BlockHeader();
            blockHeader.Type = BlockType.START_OF_STREAM;
            payload.BlockHeaders.Add(blockHeader);

            payload.StartOfStream = new StartOfStream();

            byte[] buffer = new byte[payload.CalculateSize()];
            payload.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded TIA p25 start frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);
        }

        /// <summary>
        /// Helper to send DFSI end of stream.
        /// </summary>
        /// <remarks>This implements TIA-102.BAHA standard DFSI RTP frame handling.</remarks>
        private void TIA_DFSIEndOfStream()
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            P25RTPPayload payload = new P25RTPPayload();
            payload.Control.Signal = true;

            BlockHeader blockHeader = new BlockHeader();
            blockHeader.Type = BlockType.END_OF_STREAM;
            payload.BlockHeaders.Add(blockHeader);

            byte[] buffer = new byte[payload.CalculateSize()];
            payload.Encode(ref buffer);
            Log.Logger.Debug($"({Program.Configuration.Name}) encoded TIA p25 end frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

            if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                dfsiRTP.SendRemote(buffer);
            else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                dfsiSerial.Send(buffer);
        }

        /// <summary>
        /// Helper to send P25 IMBE frames as DFSI frames.
        /// </summary>
        /// <remarks>This implements "the" manufacturer standard DFSI RTP frame handling.</remarks>
        /// <param name="duid"></param>
        /// <param name="ldu"></param>
        /// <param name="e"></param>
        private void Mot_DFSISendFrame(P25DUID duid, byte[] ldu, P25DataReceivedEvent e)
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            // generate RS bytes depending on DUID
            byte[] rs = new byte[18];
            switch (duid) {
                case P25DUID.LDU1:
                {
                    rs[0U] = e.Data[4];                                                             // LCO
                    rs[1U] = e.Data[5];                                                             // MFId
                    rs[2U] = ldu[53];                                                               // Service Options
                    FneUtils.Write3Bytes(e.DstId, ref rs, 3);                                       // Target Address
                    FneUtils.Write3Bytes(e.SrcId, ref rs, 6);                                       // Source Address
                    rs = ReedSolomonAlgorithm.Encode(rs, ErrorCorrectionCodeType.ReedSolomon_241213);
                }
                break;
                case P25DUID.LDU2:
                {
                    rs[0] = ldu[51];                                                                // Message Indicator
                    rs[1] = ldu[52];
                    rs[2] = ldu[53];
                    rs[3] = ldu[76];
                    rs[4] = ldu[77];
                    rs[5] = ldu[78];
                    rs[6] = ldu[101];
                    rs[7] = ldu[102];
                    rs[8] = ldu[103];

                    rs[9U] = ldu[126];                                                              // Algorithm ID
                    rs[10U] = ldu[127];                                                             // Key ID
                    rs[11U] = ldu[128];                                                             // ...

                    rs = ReedSolomonAlgorithm.Encode(rs, ErrorCorrectionCodeType.ReedSolomon_24169);
                }
                break;
            }

            // decode 9 IMBE codewords into PCM samples
            for (int n = 0; n < 9; n++)
            {
                byte[] buffer = null;
                MotFullRateVoice voice = new MotFullRateVoice();

                switch (n)
                {
                    case 0:     // VOICE1 / 10
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE1 : P25DFSI.P25_DFSI_LDU2_VOICE10;
                            MotStartVoiceFrame startVoice = new MotStartVoiceFrame();
                            startVoice.StartOfStream = new MotStartOfStream();
                            startVoice.StartOfStream.StartStop = StartStopFlag.START;
                            startVoice.StartOfStream.RT = RTFlag.ENABLED;   // TODO: make this selectable
                            startVoice.FullRateVoice = new MotFullRateVoice();
                            startVoice.FullRateVoice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE1 : P25DFSI.P25_DFSI_LDU2_VOICE10;
                            Buffer.BlockCopy(ldu, 10, startVoice.FullRateVoice.IMBE, 0, IMBE_BUF_LEN);

                            buffer = new byte[MotStartVoiceFrame.LENGTH];
                            startVoice.Encode(ref buffer);
                        }
                        break;
                    case 1:     // VOICE2 / 11
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE2 : P25DFSI.P25_DFSI_LDU2_VOICE11;
                            Buffer.BlockCopy(ldu, 26, voice.IMBE, 0, IMBE_BUF_LEN);
                        }
                        break;
                    case 2:     // VOICE3 / 12
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE3 : P25DFSI.P25_DFSI_LDU2_VOICE12;
                            Buffer.BlockCopy(ldu, 55, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[51];     // LCO
                                        voice.AdditionalFrameData[1] = ldu[52];     // MFId
                                        voice.AdditionalFrameData[2] = ldu[53];     // Service Options
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[51];     // MI
                                        voice.AdditionalFrameData[1] = ldu[52];
                                        voice.AdditionalFrameData[2] = ldu[53];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 3:     // VOICE4 / 13
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE4 : P25DFSI.P25_DFSI_LDU2_VOICE13;
                            Buffer.BlockCopy(ldu, 80, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = e.Data[8];   // Destination Address
                                        voice.AdditionalFrameData[1] = e.Data[9];
                                        voice.AdditionalFrameData[2] = e.Data[10];
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[76];     // MI
                                        voice.AdditionalFrameData[1] = ldu[77];
                                        voice.AdditionalFrameData[2] = ldu[78];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 4:     // VOICE5 / 14
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE5 : P25DFSI.P25_DFSI_LDU2_VOICE14;
                            Buffer.BlockCopy(ldu, 105, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = e.Data[5];   // Source Address
                                        voice.AdditionalFrameData[1] = e.Data[6];
                                        voice.AdditionalFrameData[2] = e.Data[7];
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[101];    // MI
                                        voice.AdditionalFrameData[1] = ldu[102];
                                        voice.AdditionalFrameData[2] = ldu[103];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 5:     // VOICE6 / 15
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE6 : P25DFSI.P25_DFSI_LDU2_VOICE15;
                            Buffer.BlockCopy(ldu, 130, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[9];       // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[10];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[11];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = ldu[126];    // Algorithm ID
                                        voice.AdditionalFrameData[1] = ldu[127];    // Key ID
                                        voice.AdditionalFrameData[2] = ldu[128];    // ...
                                    }
                                    break;
                            }
                        }
                        break;
                    case 6:     // VOICE7 / 16
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE7 : P25DFSI.P25_DFSI_LDU2_VOICE16;
                            Buffer.BlockCopy(ldu, 155, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[12];      // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[13];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[14];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[12];      // RS (24,16,9)
                                        voice.AdditionalFrameData[1] = rs[13];      // RS (24,16,9)
                                        voice.AdditionalFrameData[2] = rs[14];      // RS (24,16,9)
                                    }
                                    break;
                            }
                        }
                        break;
                    case 7:     // VOICE8 / 17
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE8 : P25DFSI.P25_DFSI_LDU2_VOICE17;
                            Buffer.BlockCopy(ldu, 180, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[15];      // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[16];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[17];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[15];      // RS (24,16,9)
                                        voice.AdditionalFrameData[1] = rs[16];      // RS (24,16,9)
                                        voice.AdditionalFrameData[2] = rs[17];      // RS (24,16,9)
                                    }
                                    break;
                            }
                        }
                        break;
                    case 8:     // VOICE9 / 18
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE9 : P25DFSI.P25_DFSI_LDU2_VOICE18;
                            Buffer.BlockCopy(ldu, 204, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                            voice.AdditionalFrameData[0] = e.Data[20U];             // LSD 1
                            voice.AdditionalFrameData[1] = e.Data[21U];             // LSD 2
                        }
                        break;
                }

                if (n != 0)
                {
                    buffer = new byte[voice.Size()];
                    voice.Encode(ref buffer);
                }

                Log.Logger.Debug($"({Program.Configuration.Name}) encoded mot p25 voice frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

                if (buffer != null)
                    if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                        dfsiRTP.SendRemote(buffer);
                    else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                        dfsiSerial.Send(buffer);
            }
        }

        /// <summary>
        /// Helper to send P25 IMBE frames as DFSI frames.
        /// </summary>
        /// <remarks>This implements TIA-102.BAHA standard DFSI RTP frame handling.</remarks>
        /// <param name="duid"></param>
        /// <param name="ldu"></param>
        /// <param name="e"></param>
        private void TIA_DFSISendFrame(P25DUID duid, byte[] ldu, P25DataReceivedEvent e)
        {
            if (!Program.Configuration.NoConnectionEstablishment && Program.Configuration.Mode == DFSIMode.UdpDvm)
            {
                if (!dfsiControl.IsConnected)
                    return;
            }

            // generate RS bytes depending on DUID
            byte[] rs = new byte[18];
            switch (duid) {
                case P25DUID.LDU1:
                {
                    rs[0U] = ldu[51];                                                               // LCO
                    rs[1U] = ldu[52];                                                               // MFId
                    rs[2U] = ldu[53];                                                               // Service Options
                    FneUtils.Write3Bytes(e.DstId, ref rs, 3);                                       // Target Address
                    FneUtils.Write3Bytes(e.SrcId, ref rs, 6);                                       // Source Address
                    rs = ReedSolomonAlgorithm.Encode(rs, ErrorCorrectionCodeType.ReedSolomon_241213);
                }
                break;
                case P25DUID.LDU2:
                {
                    rs[0] = ldu[51];                                                                // Message Indicator
                    rs[1] = ldu[52];
                    rs[2] = ldu[53];
                    rs[3] = ldu[76];
                    rs[4] = ldu[77];
                    rs[5] = ldu[78];
                    rs[6] = ldu[101];
                    rs[7] = ldu[102];
                    rs[8] = ldu[103];

                    rs[9U] = ldu[126];                                                              // Algorithm ID
                    rs[10U] = ldu[127];                                                             // Key ID
                    rs[11U] = ldu[128];                                                             // ...

                    rs = ReedSolomonAlgorithm.Encode(rs, ErrorCorrectionCodeType.ReedSolomon_24169);
                }
                break;
            }

            // decode 9 IMBE codewords into PCM samples
            for (int n = 0; n < 9; n++)
            {
                P25RTPPayload payload = new P25RTPPayload();
                payload.Control.Signal = true;

                BlockHeader blockHeader = new BlockHeader();
                blockHeader.Type = BlockType.FULL_RATE_VOICE;
                payload.BlockHeaders.Add(blockHeader);

                FullRateVoice voice = new FullRateVoice();
                voice.IMBE = new byte[IMBE_BUF_LEN];

                switch (n)
                {
                    case 0:     // VOICE1 / 10
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE1 : P25DFSI.P25_DFSI_LDU2_VOICE10;
                            Buffer.BlockCopy(ldu, 10, voice.IMBE, 0, IMBE_BUF_LEN);
                        }
                        break;
                    case 1:     // VOICE2 / 11
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE2 : P25DFSI.P25_DFSI_LDU2_VOICE11;
                            Buffer.BlockCopy(ldu, 26, voice.IMBE, 0, IMBE_BUF_LEN);
                        }
                        break;
                    case 2:     // VOICE3 / 12
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE3 : P25DFSI.P25_DFSI_LDU2_VOICE12;
                            Buffer.BlockCopy(ldu, 55, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[3];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[51];     // LCO
                                        voice.AdditionalFrameData[1] = ldu[52];     // MFId
                                        voice.AdditionalFrameData[2] = ldu[53];     // Service Options
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[51];     // MI
                                        voice.AdditionalFrameData[1] = ldu[52];
                                        voice.AdditionalFrameData[2] = ldu[53];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 3:     // VOICE4 / 13
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE4 : P25DFSI.P25_DFSI_LDU2_VOICE13;
                            Buffer.BlockCopy(ldu, 80, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[3];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = e.Data[8];   // Destination Address
                                        voice.AdditionalFrameData[1] = e.Data[9];
                                        voice.AdditionalFrameData[2] = e.Data[10];
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[76];     // MI
                                        voice.AdditionalFrameData[1] = ldu[77];
                                        voice.AdditionalFrameData[2] = ldu[78];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 4:     // VOICE5 / 14
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE5 : P25DFSI.P25_DFSI_LDU2_VOICE14;
                            Buffer.BlockCopy(ldu, 105, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[3];
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData[0] = e.Data[5];   // Source Address
                                        voice.AdditionalFrameData[1] = e.Data[6];
                                        voice.AdditionalFrameData[2] = e.Data[7];
                                    }
                                    break;

                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData[0] = ldu[101];    // MI
                                        voice.AdditionalFrameData[1] = ldu[102];
                                        voice.AdditionalFrameData[2] = ldu[103];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 5:     // VOICE6 / 15
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE6 : P25DFSI.P25_DFSI_LDU2_VOICE15;
                            Buffer.BlockCopy(ldu, 130, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[9];       // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[10];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[11];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[3];
                                        voice.AdditionalFrameData[0] = ldu[126];    // Algorithm ID
                                        voice.AdditionalFrameData[1] = ldu[127];    // Key ID
                                        voice.AdditionalFrameData[2] = ldu[128];
                                    }
                                    break;
                            }
                        }
                        break;
                    case 6:     // VOICE7 / 16
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE7 : P25DFSI.P25_DFSI_LDU2_VOICE16;
                            Buffer.BlockCopy(ldu, 155, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[12];      // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[13];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[14];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[12];      // RS (24,16,9)
                                        voice.AdditionalFrameData[1] = rs[13];      // RS (24,16,9)
                                        voice.AdditionalFrameData[2] = rs[14];      // RS (24,16,9)
                                    }
                                    break;
                            }
                        }
                        break;
                    case 7:     // VOICE8 / 17
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE8 : P25DFSI.P25_DFSI_LDU2_VOICE17;
                            Buffer.BlockCopy(ldu, 180, voice.IMBE, 0, IMBE_BUF_LEN);
                            switch (duid)
                            {
                                case P25DUID.LDU1:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[15];      // RS (24,12,13)
                                        voice.AdditionalFrameData[1] = rs[16];      // RS (24,12,13)
                                        voice.AdditionalFrameData[2] = rs[17];      // RS (24,12,13)
                                    }
                                    break;
                                case P25DUID.LDU2:
                                    {
                                        voice.AdditionalFrameData = new byte[MotFullRateVoice.ADDTL_LENGTH];
                                        voice.AdditionalFrameData[0] = rs[15];      // RS (24,16,9)
                                        voice.AdditionalFrameData[1] = rs[16];      // RS (24,16,9)
                                        voice.AdditionalFrameData[2] = rs[17];      // RS (24,16,9)
                                    }
                                    break;
                            }
                        }
                        break;
                    case 8:     // VOICE9 / 18
                        {
                            voice.FrameType = duid == P25DUID.LDU1 ? P25DFSI.P25_DFSI_LDU1_VOICE9 : P25DFSI.P25_DFSI_LDU2_VOICE18;
                            Buffer.BlockCopy(ldu, 204, voice.IMBE, 0, IMBE_BUF_LEN);
                            voice.AdditionalFrameData = new byte[2];
                            voice.AdditionalFrameData[0] = e.Data[20U];             // LSD 1
                            voice.AdditionalFrameData[1] = e.Data[21U];             // LSD 2
                        }
                        break;
                }

                payload.FullRateVoiceBlocks.Add(voice);

                byte[] buffer = new byte[payload.CalculateSize()];
                payload.Encode(ref buffer);
                Log.Logger.Debug($"({Program.Configuration.Name}) encoded TIA p25 frame ({buffer.Length}) {BitConverter.ToString(buffer).Replace("-", " ")}");

                if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                    dfsiRTP.SendRemote(buffer);
                else if (Program.Configuration.Mode == DFSIMode.SerialDvm)
                    dfsiSerial.Send(buffer);
            }
        }

        /// <summary>
        /// Event handler used to process incoming P25 data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void P25DataReceived(object sender, P25DataReceivedEvent e)
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

                if (remoteCallInProgress)
                    return;

                // is this a new call stream?
                if (e.StreamId != RxStreamId && ((e.DUID != P25DUID.TDU) && (e.DUID != P25DUID.TDULC)))
                {
                    callInProgress = true;
                    RxStart = pktTime;
                    Log.Logger.Information($"({SystemName}) P25D: Traffic *CALL START     * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} [STREAM ID {e.StreamId}]");
                    if (Program.Configuration.Mode == DFSIMode.UdpDvm)
                        dfsiRTP.pktSeq(true);
                    if (Program.Configuration.TheManufacturer)
                        Mot_DFSIStartOfStream(e);
                    else
                        TIA_DFSIStartOfStream(e);
                }

                if (((e.DUID == P25DUID.TDU) || (e.DUID == P25DUID.TDULC)) && (RxType != FrameType.TERMINATOR))
                {
                    callInProgress = false;
                    TimeSpan callDuration = pktTime - RxStart;
                    Log.Logger.Information($"({SystemName}) P25D: Traffic *CALL END       * PEER {e.PeerId} SRC_ID {e.SrcId} TGID {e.DstId} DUR {callDuration} [STREAM ID {e.StreamId}]");
                    if (Program.Configuration.TheManufacturer)
                        Mot_DFSIEndOfStream();
                    else
                        TIA_DFSIEndOfStream();
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

                                // send 9 IMBE codewords over DFSI
                                if (Program.Configuration.TheManufacturer)
                                    Mot_DFSISendFrame(P25DUID.LDU1, netLDU1, e);
                                else
                                    TIA_DFSISendFrame(P25DUID.LDU1, netLDU1, e);
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

                                // send 9 IMBE codewords over DFSI
                                if (Program.Configuration.TheManufacturer)
                                    Mot_DFSISendFrame(P25DUID.LDU2, netLDU2, e);
                                else
                                    TIA_DFSISendFrame(P25DUID.LDU2, netLDU2, e);
                            }
                        }
                        break;
                }

                RxType = e.FrameType;
                RxStreamId = e.StreamId;
            }
            else
                Log.Logger.Warning($"({SystemName}) P25D: DFSI does not support private calls.");

            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
} // namespace dvmdfsi
