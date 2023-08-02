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
    /// 
    /// </summary>
    public class FSCResponse
    {
        private byte version;

        /// <summary>
        /// Length of response.
        /// </summary>
        public virtual uint Length { get => 1; }

        /// <summary>
        /// Message Version.
        /// </summary>
        public byte Version { get => version; }

        /*
        ** Methods
        */
        /// <summary>
        /// Initializes a new instance of the <see cref="FSCResponse"/> class.
        /// </summary>
        public FSCResponse()
        {
            version = 1;
        }

        /// <summary>
        /// Decode a FSC response header.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool Decode(byte[] data)
        {
            if (data == null)
                return false;

            version = (byte)(data[0U]);                                         // Message Version

            return true;
        }

        /// <summary>
        /// Encode a FSC response header.
        /// </summary>
        /// <param name="data"></param>
        public virtual void Encode(ref byte[] data)
        {
            if (data == null)
                return;

            data[0U] = (byte)version;                                           // Message Version
        }
    } // public class FSCResponse
} // namespace dvmdfsi.DFSI.FSC
