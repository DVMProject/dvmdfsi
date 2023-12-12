﻿/**
* Digital Voice Modem - DFSI
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / DFSI
*
*/
/*
*   Copyright (C) 2022-2023 by Bryan Biedenkapp N2PLL
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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Serilog;

using fnecore;
using fnecore.DMR;
using fnecore.P25;

using dvmdfsi.DFSI;
using dvmdfsi.DFSI.FSC;
using dvmdfsi.DFSI.RTP;
using System.Diagnostics.Tracing;

namespace dvmdfsi
{
    /// <summary>
    /// Metadata class containing remote call data.
    /// </summary>
    public class RemoteCallData
    {
        /// <summary>
        /// Source ID.
        /// </summary>
        public uint SrcId = 0;
        /// <summary>
        /// Destination ID.
        /// </summary>
        public uint DstId = 0;

        /// <summary>
        /// Link-Control Opcode.
        /// </summary>
        public byte LCO = 0;
        /// <summary>
        /// Manufacturer ID.
        /// </summary>
        public byte MFId = 0;
        /// <summary>
        /// Service Options.
        /// </summary>
        public byte ServiceOptions = 0;

        /// <summary>
        /// Low-speed Data Byte 1
        /// </summary>
        public byte LSD1 = 0;
        /// <summary>
        /// Low-speed Data Byte 2
        /// </summary>
        public byte LSD2 = 0;

        /// <summary>
        /// Encryption Message Indicator
        /// </summary>
        public byte[] MessageIndicator = new byte[P25Defines.P25_MI_LENGTH];

        /// <summary>
        /// Algorithm ID.
        /// </summary>
        public byte AlgorithmId = P25Defines.P25_ALGO_UNENCRYPT;
        /// <summary>
        /// Key ID.
        /// </summary>
        public ushort KeyId = 0;

        /*
        ** Methods
        */

        /// <summary>
        /// Reset values.
        /// </summary>
        public void Reset()
        {
            SrcId = 0;
            DstId = 0;

            LCO = 0;
            MFId = 0;
            ServiceOptions = 0;

            LSD1 = 0;
            LSD2 = 0;

            MessageIndicator = new byte[P25Defines.P25_MI_LENGTH];

            AlgorithmId = P25Defines.P25_ALGO_UNENCRYPT;
            KeyId = 0;
        }
    } // public class RemoteCallData

    /// <summary>
    /// Implements a FNE system.
    /// </summary>
    public abstract partial class FneSystemBase
    {
        protected FneBase fne;

        private Random rand;
        private uint txStreamId;

        private bool callInProgress = false;
        
        private bool remoteCallInProgress = false;
        public RemoteCallData remoteCallData = new RemoteCallData();

        private ControlService dfsiControl;
        private RTPService dfsiRTP;
        private SerialService dfsiSerial;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the system name for this <see cref="FneSystemBase"/>.
        /// </summary>
        public string SystemName
        {
            get
            {
                if (fne != null)
                    return fne.SystemName;
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the peer ID for this <see cref="FneSystemBase"/>.
        /// </summary>
        public uint PeerId
        {
            get
            {
                if (fne != null)
                    return fne.PeerId;
                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Flag indicating whether this <see cref="FneSystemBase"/> is running.
        /// </summary>
        public bool IsStarted
        { 
            get
            {
                if (fne != null)
                    return fne.IsStarted;
                return false;
            }
        }

        /// <summary>
        /// Gets the <see cref="FneType"/> this <see cref="FneBase"/> is.
        /// </summary>
        public FneType FneType
        {
            get
            {
                if (fne != null)
                    return fne.FneType;
                return FneType.UNKNOWN;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="FneSystemBase"/> class.
        /// </summary>
        /// <param name="fne">Instance of <see cref="FneMaster"/> or <see cref="FnePeer"/></param>
        public FneSystemBase(FneBase fne)
        {
            this.fne = fne;

            this.rand = new Random(Guid.NewGuid().GetHashCode());

            // hook various FNE network callbacks
            this.fne.DMRDataValidate = DMRDataValidate;
            this.fne.DMRDataReceived += DMRDataReceived;

            this.fne.P25DataValidate = P25DataValidate;
            this.fne.P25DataPreprocess += P25DataPreprocess;
            this.fne.P25DataReceived += P25DataReceived;

            this.fne.NXDNDataValidate = NXDNDataValidate;
            this.fne.NXDNDataReceived += NXDNDataReceived;

            this.fne.PeerIgnored = PeerIgnored;
            this.fne.PeerConnected += PeerConnected;

            // hook logger callback
            this.fne.LogLevel = Program.FneLogLevel;
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
                // skip doing anything with this for now...
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
        public void Start()
        {
            if (!fne.IsStarted)
                fne.Start();

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
            {
                dfsiSerial.Start();
            }
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
        public void Stop()
        {
            if (fne.IsStarted)
                fne.Stop();

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
        protected virtual bool PeerIgnored(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId)
        {
            return false;
        }

        /// <summary>
        /// Event handler used to handle a peer connected event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void PeerConnected(object sender, PeerConnectedEvent e)
        {
            return;
        }
    } // public abstract partial class FneSystemBase
} // namespace dvmdfsi
