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
