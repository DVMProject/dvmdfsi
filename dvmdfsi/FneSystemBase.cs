// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*   Copyright (C) 2023 Patrick McDonnell, W3AXL
*
*/
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Serilog;

using fnecore;
using fnecore.EDAC.RS;
using fnecore.DMR;
using fnecore.P25;

using dvmdfsi.DFSI;
using dvmdfsi.DFSI.FSC;
using dvmdfsi.DFSI.RTP;

namespace dvmdfsi
{
    /// <summary>
    /// Metadata class containing remote call data.
    /// </summary>
    public class RemoteCallData : fnecore.RemoteCallData
    {
        /// <summary>
        /// Voice Header 1
        /// </summary>
        public byte[] VHDR1 = null;
        /// <summary>
        /// Voice Header 2
        /// </summary>
        public byte[] VHDR2 = null;

        /*
        ** Methods
        */

        /// <summary>
        /// Reset values.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            VHDR1 = null;
            VHDR2 = null;
        }
    } // public class RemoteCallData : fnecore.RemoteCallData

    /// <summary>
    /// Implements a FNE system.
    /// </summary>
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {
        private Random rand;
        private uint txStreamId;

        private bool callInProgress = false;
        
        private bool remoteCallInProgress = false;
        public RemoteCallData remoteCallData = new RemoteCallData();

        private ControlService dfsiControl;
        private RTPService dfsiRTP;
        private SerialService dfsiSerial;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="fne">Instance of <see cref="FneMaster"/> or <see cref="FnePeer"/></param>
        public FneSystemBase(FneBase fne) : base(fne, Program.FneLogLevel)
        {
            this.fne = fne;

            this.rand = new Random(Guid.NewGuid().GetHashCode());

            // hook logger callback
            this.fne.Logger = (LogLevel level, string message) =>
            {
                switch (level)
                {
                    case LogLevel.WARNING:
                        Log.Logger.Warning(message);
                        break;
                    case LogLevel.ERROR:
                        Log.Logger.Error(message);
                        break;
                    case LogLevel.DEBUG:
                        Log.Logger.Debug(message);
                        break;
                    case LogLevel.FATAL:
                        Log.Logger.Fatal(message);
                        break;
                    case LogLevel.INFO:
                    default:
                        Log.Logger.Information(message);
                        break;
                }
            };

            netLDU1 = new byte[9 * 25];
            netLDU2 = new byte[9 * 25];

            // Mode switch for DFSI handling
            switch (Program.Configuration.Mode)
            {
                case DFSIMode.None:
                    Log.Logger.Error("No DFSI mode specified!");
                    break;
                // UDP DFSI to DVM FNE
                case DFSIMode.UdpDvm:
                    this.dfsiControl = new ControlService();
                    this.dfsiRTP = new RTPService();
                    if (!Program.Configuration.TheManufacturer)
                        this.dfsiRTP.RTPFrameHandler += TIA_DfsiRTP_RTPFrameHandler;
                    else
                        this.dfsiRTP.RTPFrameHandler += Mot_DfsiRTP_RTPFrameHandler;
                    break;
                // Serial DFSI to DVM FNE
                case DFSIMode.SerialDvm:
                    this.dfsiSerial = new SerialService(Program.Configuration.SerialPortName, Program.Configuration.SerialBaudrate);
                    if (!Program.Configuration.TheManufacturer)
                        this.dfsiSerial.RTPFrameHandler += TIA_DfsiRTP_RTPFrameHandler;
                    else
                        this.dfsiSerial.RTPFrameHandler += Mot_DfsiRTP_RTPFrameHandler;
                    break;
                // Serial DFSI to UDP DFSI (TODO: Implement this lol)
                case DFSIMode.SerialUdp:
                    Log.Logger.Error("Serial DFSI to UDP DFSI not yet implemented!");
                    break;
                default:
                    Log.Logger.Error($"Unknown DFSI mode specified: {Program.Configuration.Mode}");
                    break;
            }
        }

        /// <summary>
        /// DFSI RTP Frame Handler.
        /// </summary>
        /// <remarks>This implements "the" manufacturer standard DFSI RTP frame handling.</remarks>
        /// <param name="frame"></param>
        /// <param name="message"></param>
        /// <param name="rtpHeader"></param>
        /// <returns></returns>
        private void Mot_DfsiRTP_RTPFrameHandler(UdpFrame frame, byte[] message, RtpHeader rtpHeader)
        {
            if (callInProgress)
                return;

            byte frameType = message[0U];
            if (frameType == P25DFSI.P25_DFSI_MOT_START_STOP)
            {
                MotStartOfStream start = new MotStartOfStream(message);
                if (start.StartStop == StartStopFlag.START)
                {
                    if (txStreamId == 0)
                    {
                        txStreamId = (uint)rand.Next(int.MinValue, int.MaxValue);
                        remoteCallInProgress = true;
                        remoteCallData.Reset();
                        Log.Logger.Information($"({SystemName}) DFSI Traffic *CALL START     * [STREAM ID {txStreamId}]");
                        SendP25TDU(remoteCallData, true);
                    }
                }
                else
                {
                    Log.Logger.Information($"({SystemName}) DFSI Traffic *CALL END       * [STREAM ID {txStreamId}]");
                    SendP25TDU(remoteCallData);
                    txStreamId = 0;
                    remoteCallInProgress = false;
                    remoteCallData.Reset();
                }
            }
            else if (frameType == P25DFSI.P25_DFSI_MOT_VHDR_1 || frameType == P25DFSI.P25_DFSI_MOT_VHDR_2)
            {
                if (frameType == P25DFSI.P25_DFSI_MOT_VHDR_1)
                {
                    MotVoiceHeader1 vhdr1 = new MotVoiceHeader1(message);
                    remoteCallData.VHDR1 = vhdr1.Header;
                }
                else
                {
                    MotVoiceHeader2 vhdr2 = new MotVoiceHeader2(message);
                    remoteCallData.VHDR2 = vhdr2.Header;
                    if (remoteCallData.VHDR1 != null &&
                        remoteCallData.VHDR2 != null)
                    {
                        byte[] raw = new byte[P25DFSI.P25_DFSI_VHDR_RAW_LEN];

                        Buffer.BlockCopy(remoteCallData.VHDR1, 0, raw, 0, 8);
                        Buffer.BlockCopy(remoteCallData.VHDR1, 9, raw, 8, 8);
                        Buffer.BlockCopy(remoteCallData.VHDR1, 18, raw, 16, 2);

                        Buffer.BlockCopy(remoteCallData.VHDR2, 0, raw, 18, 8);
                        Buffer.BlockCopy(remoteCallData.VHDR2, 9, raw, 26, 8);
                        Buffer.BlockCopy(remoteCallData.VHDR2, 18, raw, 34, 2);

                        byte[] vhdr = new byte[P25DFSI.P25_DFSI_VHDR_LEN];
                        uint offset = 0;
                        for (int i = 0; i < raw.Length; i++, offset += 6)
                            FneUtils.HEX2BIN(raw[i], ref vhdr, offset);

                        vhdr = ReedSolomonAlgorithm.Decode(vhdr, ErrorCorrectionCodeType.ReedSolomon_362017);

                        byte[] mi = new byte[P25Defines.P25_MI_LENGTH];
                        Buffer.BlockCopy(vhdr, 0, mi, 0, P25Defines.P25_MI_LENGTH);
                        remoteCallData.MessageIndicator = mi;                // MI
                        remoteCallData.MFId = vhdr[9];                       // Manufactuerer ID
                        remoteCallData.AlgorithmId = vhdr[10];               // Algorithm ID
                        remoteCallData.KeyId = FneUtils.ToUInt16(vhdr, 11);  // Key ID
                        remoteCallData.DstId = FneUtils.ToUInt16(vhdr, 13);  // TGID
                    }
                }
            }
            else
            {
                if (frameType == P25DFSI.P25_DFSI_LDU1_VOICE1 || frameType == P25DFSI.P25_DFSI_LDU2_VOICE10)
                {
                    MotStartVoiceFrame voice = new MotStartVoiceFrame(message);
                    switch (frameType)
                    {
                        // LDU1
                        case P25DFSI.P25_DFSI_LDU1_VOICE1:
                            Buffer.BlockCopy(voice.FullRateVoice.IMBE, 0, netLDU1, 10, IMBE_BUF_LEN);
                            break;

                        // LDU2
                        case P25DFSI.P25_DFSI_LDU2_VOICE10:
                            Buffer.BlockCopy(voice.FullRateVoice.IMBE, 0, netLDU2, 10, IMBE_BUF_LEN);
                            break;
                    }
                }
                else
                {
                    MotFullRateVoice voice = new MotFullRateVoice(message);
                    switch (frameType)
                    {
                        // LDU1
                        case P25DFSI.P25_DFSI_LDU1_VOICE2:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 26, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE3:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 55, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                    {
                                        remoteCallData.LCO = voice.AdditionalFrameData[0];
                                        remoteCallData.MFId = voice.AdditionalFrameData[1];
                                        remoteCallData.ServiceOptions = voice.AdditionalFrameData[3];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC3 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE4:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 80, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                        remoteCallData.DstId = FneUtils.Bytes3ToUInt32(voice.AdditionalFrameData, 0);
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC4 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE5:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 105, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                        remoteCallData.SrcId = FneUtils.Bytes3ToUInt32(voice.AdditionalFrameData, 0);
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC5 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE6:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 130, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE7:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 155, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE8:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 180, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU1_VOICE9:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 204, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 2)
                                    {
                                        remoteCallData.LSD1 = voice.AdditionalFrameData[0];
                                        remoteCallData.LSD2 = voice.AdditionalFrameData[1];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC9 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;

                        // LDU2
                        case P25DFSI.P25_DFSI_LDU2_VOICE11:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 26, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE12:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 55, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                    {
                                        remoteCallData.MessageIndicator[0] = voice.AdditionalFrameData[0];
                                        remoteCallData.MessageIndicator[1] = voice.AdditionalFrameData[1];
                                        remoteCallData.MessageIndicator[2] = voice.AdditionalFrameData[2];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC12 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE13:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 80, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                    {
                                        remoteCallData.MessageIndicator[3] = voice.AdditionalFrameData[0];
                                        remoteCallData.MessageIndicator[4] = voice.AdditionalFrameData[1];
                                        remoteCallData.MessageIndicator[5] = voice.AdditionalFrameData[2];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC13 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE14:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 105, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                    {
                                        remoteCallData.MessageIndicator[6] = voice.AdditionalFrameData[0];
                                        remoteCallData.MessageIndicator[7] = voice.AdditionalFrameData[1];
                                        remoteCallData.MessageIndicator[8] = voice.AdditionalFrameData[2];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC14 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE15:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 130, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 3)
                                    {
                                        remoteCallData.AlgorithmId = voice.AdditionalFrameData[0];
                                        remoteCallData.KeyId = FneUtils.ToUInt16(voice.AdditionalFrameData, 1);
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC15 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE16:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 155, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE17:
                            Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 180, IMBE_BUF_LEN);
                            break;
                        case P25DFSI.P25_DFSI_LDU2_VOICE18:
                            {
                                Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 204, IMBE_BUF_LEN);
                                if (voice.AdditionalFrameData != null)
                                {
                                    if (voice.AdditionalFrameData.Length >= 2)
                                    {
                                        remoteCallData.LSD1 = voice.AdditionalFrameData[0];
                                        remoteCallData.LSD2 = voice.AdditionalFrameData[1];
                                    }
                                    else
                                        Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC18 Missing Metadata [STREAM ID {txStreamId}]");
                                }
                            }
                            break;
                    }
                }

                FnePeer peer = (FnePeer)fne;

                // send P25 LDU1
                if (p25N == 8U)
                {
                    ushort pktSeq = 0;
                    if (p25SeqNo == 0U)
                        pktSeq = peer.pktSeq(true);
                    else
                        pktSeq = peer.pktSeq();

                    Log.Logger.Information($"({SystemName}) P25D: Traffic *VOICE FRAME    * PEER {fne.PeerId} SRC_ID {remoteCallData.SrcId} TGID {remoteCallData.DstId} [STREAM ID {txStreamId}]");

                    byte[] buffer = new byte[200];
                    CreateP25MessageHdr((byte)P25DUID.LDU1, remoteCallData, ref buffer);
                    CreateP25LDU1Message(ref buffer, remoteCallData);

                    peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), buffer, pktSeq, txStreamId);
                }

                // send P25 LDU2
                if (p25N == 17U)
                {
                    ushort pktSeq = 0;
                    if (p25SeqNo == 0U)
                        pktSeq = peer.pktSeq(true);
                    else
                        pktSeq = peer.pktSeq();

                    Log.Logger.Information($"({SystemName}) P25D: Traffic *VOICE FRAME    * PEER {fne.PeerId} SRC_ID {remoteCallData.SrcId} TGID {remoteCallData.DstId} [STREAM ID {txStreamId}]");

                    byte[] buffer = new byte[200];
                    CreateP25MessageHdr((byte)P25DUID.LDU2, remoteCallData, ref buffer);
                    CreateP25LDU2Message(ref buffer, remoteCallData);

                    peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), buffer, pktSeq, txStreamId);

                    // Reset the p25N counter to start the next LDU
                    p25N = 0;
                }
                else
                {
                    p25N++;
                }

                p25SeqNo++;
            }
        }

        /// <summary>
        /// DFSI RTP Frame Handler.
        /// </summary>
        /// <remarks>This implements TIA-102.BAHA standard DFSI RTP frame handling.</remarks>
        /// <param name="frame"></param>
        /// <param name="message"></param>
        /// <param name="rtpHeader"></param>
        /// <returns></returns>
        private void TIA_DfsiRTP_RTPFrameHandler(UdpFrame frame, byte[] message, RtpHeader rtpHeader)
        {
            if (callInProgress)
                return;

            P25RTPPayload payload = new P25RTPPayload(message);
            for (int i = 0; i < payload.BlockHeaders.Count; i++)
            {
                BlockHeader header = payload.BlockHeaders[i];
                switch (header.Type)
                {
                    case BlockType.START_OF_STREAM:
                        {
                            if (txStreamId == 0)
                            {
                                txStreamId = (uint)rand.Next(int.MinValue, int.MaxValue);
                                remoteCallInProgress = true;
                                remoteCallData.Reset();
                                Log.Logger.Information($"({SystemName}) DFSI Traffic *CALL START     * [STREAM ID {txStreamId}]");
                                SendP25TDU(remoteCallData, true);
                            }
                        }
                        break;
                    case BlockType.END_OF_STREAM:
                        {
                            Log.Logger.Information($"({SystemName}) DFSI Traffic *CALL END       * [STREAM ID {txStreamId}]");
                            txStreamId = 0;
                            remoteCallInProgress = false;
                            remoteCallData.Reset();
                        }
                        break;

                    case BlockType.FULL_RATE_VOICE:
                        {
                            int blkIdx = payload.BlockHeaderToVoiceBlock[i];
                            FullRateVoice voice = payload.FullRateVoiceBlocks[blkIdx];

                            switch (voice.FrameType)
                            {
                                // LDU1
                                case P25DFSI.P25_DFSI_LDU1_VOICE1:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 10, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE2:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 26, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE3:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 55, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                            {
                                                remoteCallData.LCO = voice.AdditionalFrameData[0];
                                                remoteCallData.MFId = voice.AdditionalFrameData[1];
                                                remoteCallData.ServiceOptions = voice.AdditionalFrameData[3];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC3 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE4:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 80, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                                remoteCallData.DstId = FneUtils.Bytes3ToUInt32(voice.AdditionalFrameData, 0);
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC4 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE5:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 105, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                                remoteCallData.SrcId = FneUtils.Bytes3ToUInt32(voice.AdditionalFrameData, 0);
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC5 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE6:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 130, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE7:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 155, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE8:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 180, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU1_VOICE9:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU1, 204, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 2)
                                            {
                                                remoteCallData.LSD1 = voice.AdditionalFrameData[0];
                                                remoteCallData.LSD2 = voice.AdditionalFrameData[1];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC9 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;

                                // LDU2
                                case P25DFSI.P25_DFSI_LDU2_VOICE10:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 10, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE11:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 26, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE12:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 55, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                            {
                                                remoteCallData.MessageIndicator[0] = voice.AdditionalFrameData[0];
                                                remoteCallData.MessageIndicator[1] = voice.AdditionalFrameData[1];
                                                remoteCallData.MessageIndicator[2] = voice.AdditionalFrameData[2];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC12 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE13:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 80, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                            {
                                                remoteCallData.MessageIndicator[3] = voice.AdditionalFrameData[0];
                                                remoteCallData.MessageIndicator[4] = voice.AdditionalFrameData[1];
                                                remoteCallData.MessageIndicator[5] = voice.AdditionalFrameData[2];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC13 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE14:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 105, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                            {
                                                remoteCallData.MessageIndicator[6] = voice.AdditionalFrameData[0];
                                                remoteCallData.MessageIndicator[7] = voice.AdditionalFrameData[1];
                                                remoteCallData.MessageIndicator[8] = voice.AdditionalFrameData[2];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC14 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE15:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 130, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 3)
                                            {
                                                remoteCallData.AlgorithmId = voice.AdditionalFrameData[0];
                                                remoteCallData.KeyId = FneUtils.ToUInt16(voice.AdditionalFrameData, 1);
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC15 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE16:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 155, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE17:
                                    Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 180, IMBE_BUF_LEN);
                                    break;
                                case P25DFSI.P25_DFSI_LDU2_VOICE18:
                                    {
                                        Buffer.BlockCopy(voice.IMBE, 0, netLDU2, 204, IMBE_BUF_LEN);
                                        if (voice.AdditionalFrameData != null)
                                        {
                                            if (voice.AdditionalFrameData.Length >= 2)
                                            {
                                                remoteCallData.LSD1 = voice.AdditionalFrameData[0];
                                                remoteCallData.LSD2 = voice.AdditionalFrameData[1];
                                            }
                                            else
                                                Log.Logger.Warning($"({SystemName}) DFSI Traffic *TRAFFIC        * VC18 Missing Metadata [STREAM ID {txStreamId}]");
                                        }
                                    }
                                    break;
                            }

                            FnePeer peer = (FnePeer)fne;

                            // send P25 LDU1
                            if (p25N == 8U)
                            {
                                ushort pktSeq = 0;
                                if (p25SeqNo == 0U)
                                    pktSeq = peer.pktSeq(true);
                                else
                                    pktSeq = peer.pktSeq();

                                Log.Logger.Information($"({SystemName}) P25D: Traffic *VOICE FRAME    * PEER {fne.PeerId} SRC_ID {remoteCallData.SrcId} TGID {remoteCallData.DstId} [STREAM ID {txStreamId}]");

                                byte[] buffer = new byte[200];
                                CreateP25MessageHdr((byte)P25DUID.LDU1, remoteCallData, ref buffer);
                                CreateP25LDU1Message(ref buffer, remoteCallData);

                                peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), buffer, pktSeq, txStreamId);
                            }

                            // send P25 LDU2
                            if (p25N == 17U)
                            {
                                ushort pktSeq = 0;
                                if (p25SeqNo == 0U)
                                    pktSeq = peer.pktSeq(true);
                                else
                                    pktSeq = peer.pktSeq();

                                Log.Logger.Information($"({SystemName}) P25D: Traffic *VOICE FRAME    * PEER {fne.PeerId} SRC_ID {remoteCallData.SrcId} TGID {remoteCallData.DstId} [STREAM ID {txStreamId}]");

                                byte[] buffer = new byte[200];
                                CreateP25MessageHdr((byte)P25DUID.LDU2, remoteCallData, ref buffer);
                                CreateP25LDU2Message(ref buffer, remoteCallData);

                                peer.SendMaster(new Tuple<byte, byte>(Constants.NET_FUNC_PROTOCOL, Constants.NET_PROTOCOL_SUBFUNC_P25), buffer, pktSeq, txStreamId);
                            }

                            p25SeqNo++;
                            p25N++;
                        }
                        break;

                    default:
                        Log.Logger.Error($"Unknown/Unhandled DFSI opcode {header.Type}");
                        break;
                }
            }
        }

        /// <summary>
        /// Starts the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();

            switch (Program.Configuration.Mode)
            {
                case DFSIMode.UdpDvm:
                    StartUdpDvm();
                    break;
                case DFSIMode.SerialDvm:
                    StartSerialDvm();
                    break;
            }
        }

        /// <summary>
        /// Starts UDP DFSI to DVM FNE processes
        /// </summary>
        public void StartUdpDvm()
        {
            if (!Program.Configuration.NoConnectionEstablishment)
            {
                if (!dfsiControl.IsStarted)
                    dfsiControl.Start();

                dfsiControl.ConnectResponse += DfsiControl_ConnectResponse;

                ConnectDFSI();
            }
        }

        public void StartSerialDvm()
        {
            if (!dfsiSerial.IsStarted)
                dfsiSerial.Start();
        }

        /// <summary>
        /// Helper to send FSC_CONNECT to remote DFSI RFSS.
        /// </summary>
        private void ConnectDFSI()
        {
            FSCConnect connect = new FSCConnect();
            connect.VCBasePort = (ushort)Program.Configuration.LocalRtpPort;
            connect.VCSSRC = (uint)Program.Configuration.PeerId;
            dfsiControl.SendRemote(connect);
        }

        /// <summary>
        /// DFSI control connect response handler.
        /// </summary>
        /// <param name="response"></param>
        private void DfsiControl_ConnectResponse(FSCConnectResponse response)
        {
            dfsiRTP.ResetMasterEndpoint(response.VCBasePort);

            if (!dfsiRTP.IsStarted)
                dfsiRTP.Start();
        }

        /// <summary>
        /// Stops the main execution loop for this <see cref="FneSystemBase"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (dfsiControl != null)
            {
                if (dfsiControl.IsStarted)
                {
                    dfsiControl.SendRemote(new FSCDisconnect());
                    dfsiControl.Stop();
                }

                if (dfsiRTP.IsStarted)
                    dfsiRTP.Stop();
            }
            
            if (dfsiSerial != null)
            {
                if (dfsiSerial.IsStarted)
                    dfsiSerial.Stop();
            }
        }

        /// <summary>
        /// Callback used to process whether or not a peer is being ignored for traffic.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <returns>True, if peer is ignored, otherwise false.</returns>
        protected override bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId)
        {
            return false;
        }

        /// <summary>
        /// Event handler used to handle a peer connected event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void PeerConnected(object sender, PeerConnectedEvent e)
        {
            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
} // namespace dvmdfsi
