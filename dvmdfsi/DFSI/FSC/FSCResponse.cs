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
