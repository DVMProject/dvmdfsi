/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
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

using dvmdfsi.FNE;

namespace dvmdfsi.DFSI.FSC
{
    /// <summary>
    /// ACK/NAK Codes
    /// </summary>
    public enum AckResponseCode : byte
    {
        /// <summary>
        /// Acknowledgement.
        /// </summary>
        CONTROL_ACK = 0,
        /// <summary>
        /// Unspecified Negative Acknowledgement.
        /// </summary>
        CONTROL_NAK = 1,
        /// <summary>
        /// Server is connected to some other host.
        /// </summary>
        CONTROL_NAK_CONNECTED = 2,
        /// <summary>
        /// Unsupported Manufactuerer Message.
        /// </summary>
        CONTROL_NAK_M_UNSUPP = 3,
        /// <summary>
        /// Unsupported Message Version.
        /// </summary>
        CONTROL_NAK_V_UNSUPP = 4,
        /// <summary>
        /// Unsupported Function.
        /// </summary>
        CONTROL_NAK_F_UNSUPP = 5,
        /// <summary>
        /// Bad / Unsupported Command Parameters.
        /// </summary>
        CONTROL_NAK_PARMS = 6,
        /// <summary>
        /// FSS is currently busy with a function.
        /// </summary>
        CONTROL_NAK_BUSY = 7
    } // public enum ControlACKCode : byte

    /// <summary>
    /// 
    /// </summary>
    public class FSCACK : FSCMessage
    {
        private byte ackVersion;
        private byte ackCorrelationTag;

        /// <summary>
        /// Length of message.
        /// </summary>
        public override uint Length { get => 6; }

        /// <summary>
        /// Acknowledged Message ID.
        /// </summary>
        public MessageType AckMessageId
        {
            get;
            set;
        }

        /// <summary>
        /// Acknowledged Message Version.
        /// </summary>
        public byte AckVersion { get => ackVersion; }

        /// <summary>
        /// 
        /// </summary>
        public byte AckCorrelationTag { get => ackCorrelationTag; }

        /// <summary>
        /// Response code.
        /// </summary>
        public AckResponseCode ResponseCode
        {
            get;
            set;
        }

        /// <summary>
        /// Response Data Length.
        /// </summary>
        public byte ResponseLength
        {
            get;
            set;
        }

        /// <summary>
        /// Response Data.
        /// </summary>
        public byte[] ResponseData
        {
            get;
            set;
        }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCACK"/> class.
        /// </summary>
        public FSCACK() : base()
        {
            AckMessageId = MessageType.FSC_INVALID;
            ackVersion = 1;
            ackCorrelationTag = 0;
            ResponseCode = AckResponseCode.CONTROL_ACK;
            ResponseLength = 0;
            ResponseData = null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCACK"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FSCACK(byte[] data) : this()
        {
            Decode(data);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCACK"/> class.
        /// </summary>
        /// <param name="ackMessageId"></param>
        /// <param name="responseCode"></param>
        public FSCACK(MessageType ackMessageId, AckResponseCode responseCode) : this()
        {
            AckMessageId = ackMessageId;
            ResponseCode = responseCode;
        }

        /// <summary>
        /// Decode a FSC connect packet.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            base.Decode(data);

            AckMessageId = (MessageType)(data[2U]);                             // Ack Message ID
            ackVersion = (byte)(data[3U]);                                      // Ack Message Version
            ackCorrelationTag = (byte)(data[4U]);                               // Ack Message Correlation Tag
            ResponseCode = (AckResponseCode)(data[5U]);                         // Response Code
            ResponseLength = (byte)(data[6U]);                                  // Response Data Length

            if (ResponseLength > 0)
            {
                ResponseData = new byte[ResponseLength];
                Buffer.BlockCopy(data, 7, ResponseData, 0, ResponseLength);
            }
            else
                ResponseData = null;

            return true;
        }

        /// <summary>
        /// Encode a FSC connect packet.
        /// </summary>
        /// <param name="data"></param>
        public override void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            Encode(ref data);

            data[2U] = (byte)(AckMessageId);                                    // Ack Message ID
            data[3U] = (byte)ackVersion;                                        // Ack Message Version
            data[4U] = (byte)ackCorrelationTag;                                 // Ack Message Correlation Tag
            data[5U] = (byte)(ResponseCode);                                    // Response Code
            data[6U] = (byte)ResponseLength;                                    // Response Data Length

            if (ResponseLength > 0 && ResponseData != null)
                Buffer.BlockCopy(ResponseData, 0, data, 7, ResponseLength);
        }
    } // public class FSCConnect : FSCMessage
} // namespace dvmdfsi.DFSI.FSC
