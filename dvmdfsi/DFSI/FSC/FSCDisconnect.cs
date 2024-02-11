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

namespace dvmdfsi.DFSI.FSC
{
    /// <summary>
    /// 
    /// </summary>
    public class FSCDisconnect : FSCMessage
    {
        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCDisconnect"/> class.
        /// </summary>
        public FSCDisconnect() : base()
        {
            MessageId = MessageType.FSC_DISCONNECT;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCDisconnect"/> class.
        /// </summary>
        /// <param name="data"></param>
        public FSCDisconnect(byte[] data) : this()
        {
            Decode(data);
        }
    } // public class FSCDisconnect : FSCMessage
} // namespace dvmdfsi.DFSI.FSC
