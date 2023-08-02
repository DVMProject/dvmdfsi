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

namespace dvmdfsi.DFSI.FSC
{
    /// <summary>
    /// Control Service Message.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// Establish connection with FSS.
        /// </summary>
        FSC_CONNECT = 0,

        /// <summary>
        /// Heartbeat/Connectivity Maintenance.
        /// </summary>
        FSC_HEARTBEAT = 1,
        /// <summary>
        /// Control Service Ack.
        /// </summary>
        FSC_ACK = 2,

        /// <summary>
        /// Detach Control Service.
        /// </summary>
        FSC_DISCONNECT = 9,

        /// <summary>
        /// Invalid Control Message.
        /// </summary>
        FSC_INVALID = 127,
    } // public enum MessageType : byte

    /// <summary>
    /// 
    /// </summary>
    public class FSCMessage
    {
        private byte version;
        private byte correlationTag;

        /// <summary>
        /// Length of message.
        /// </summary>
        public virtual uint Length { get => 3; }

        /// <summary>
        /// Message ID.
        /// </summary>
        public MessageType MessageId
        {
            get;
            set;
        }

        /// <summary>
        /// Message Version.
        /// </summary>
        public byte Version { get => version; }

        /// <summary>
        /// 
        /// </summary>
        public byte CorrelationTag { get => correlationTag; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCMessage"/> class.
        /// </summary>
        public FSCMessage()
        {
            MessageId = MessageType.FSC_INVALID;
            version = 1;
            correlationTag = 0;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCMessage"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FSCMessage(byte[] data) : this()
        {
            Decode(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static FSCMessage CreateMessage(byte[] data)
        {
            if (data == null)
                return null;

            MessageType messageId = (MessageType)(data[0U]);                    // Message ID

            FSCMessage ret = null;
            switch (messageId)
            {
                case MessageType.FSC_ACK:
                    ret = new FSCACK(data);
                    break;
                case MessageType.FSC_CONNECT:
                    ret = new FSCConnect(data);
                    break;
                case MessageType.FSC_DISCONNECT:
                    ret = new FSCDisconnect(data);
                    break;
                case MessageType.FSC_HEARTBEAT:
                    ret = new FSCHeartbeat(data);
                    break;
                default:
                    ret = new FSCMessage(data);
                    break;
            }

            return ret;
        }

        /// <summary>
        /// Decode a FSC message header.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            MessageId = (MessageType)(data[0U]);                                // Message ID
            version = (byte)(data[1U]);                                         // Message Version

            if (MessageId != MessageType.FSC_HEARTBEAT && MessageId != MessageType.FSC_ACK)
                correlationTag = (byte)(data[2U]);                              // Message Correlation Tag

            return true;
        }

        /// <summary>
        /// Encode a FSC message header.
        /// </summary>
        /// <param name="data"></param>
        public virtual void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            data[0U] = (byte)(MessageId);                                       // Message ID
            data[1U] = (byte)version;                                           // Message Version

            if (MessageId != MessageType.FSC_HEARTBEAT && MessageId != MessageType.FSC_ACK)
                data[2U] = (byte)(correlationTag);                              // Message Correlation Tag
        }
    } // public class FSCMessageBase
} // namespace dvmdfsi.DFSI.FSC
