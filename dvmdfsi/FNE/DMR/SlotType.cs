﻿/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
//
// Based on code from the MMDVMHost project. (https://github.com/g4klx/MMDVMHost)
// Licensed under the GPLv2 License (https://opensource.org/licenses/GPL-2.0)
//
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

using dvmdfsi.FNE.EDAC;

namespace dvmdfsi.FNE.DMR
{
    /// <summary>
    /// Represents DMR slot type.
    /// </summary>
    public class SlotType
    {
        /// <summary>
        /// DMR access color code.
        /// </summary>
        public byte ColorCode;

        /// <summary>
        /// Slot data type.
        /// </summary>
        public byte DataType;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotType"/> class.
        /// </summary>
        public SlotType()
        {
            ColorCode = 0;
            DataType = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotType"/> class.
        /// </summary>
        /// <param name="bytes"></param>
        public SlotType(byte[] bytes)
        {
            byte[] DMRSlotType = new byte[3U];
            DMRSlotType[0U] = (byte)((bytes[12U] << 2) & 0xFCU);
            DMRSlotType[0U] |= (byte)((bytes[13U] >> 6) & 0x03U);

            DMRSlotType[1U] = (byte)((bytes[13U] << 2) & 0xC0U);
            DMRSlotType[1U] |= (byte)((bytes[19U] << 2) & 0x3CU);
            DMRSlotType[1U] |= (byte)((bytes[20U] >> 6) & 0x03U);

            DMRSlotType[2U] = (byte)((bytes[20U] << 2) & 0xF0U);

            Golay2087 golay2087 = new Golay2087();
            byte code = golay2087.Decode(DMRSlotType);

            ColorCode = (byte)((code >> 4) & 0x0FU);
            DataType = (byte)((code >> 0) & 0x0FU);
        }

        /// <summary>
        /// Gets <see cref="SlotType"/> data as bytes.
        /// </summary>
        /// <param name="bytes"></param>
        public void GetData(ref byte[] bytes)
        {
            if (bytes == null)
                throw new NullReferenceException("bytes");

            byte[] DMRSlotType = new byte[3U];
            DMRSlotType[0U] = (byte)((ColorCode << 4) & 0xF0U);
            DMRSlotType[0U] |= (byte)((DataType << 0) & 0x0FU);
            DMRSlotType[1U] = (byte)0x00U;
            DMRSlotType[2U] = (byte)0x00U;

            Golay2087 golay2087 = new Golay2087();
            golay2087.Encode(ref DMRSlotType);

            bytes[12U] = (byte)((bytes[12U] & 0xC0U) | ((DMRSlotType[0U] >> 2) & 0x3FU));
            bytes[13U] = (byte)((bytes[13U] & 0x0FU) | ((DMRSlotType[0U] << 6) & 0xC0U) | ((DMRSlotType[1U] >> 2) & 0x30U));
            bytes[19U] = (byte)((bytes[19U] & 0xF0U) | ((DMRSlotType[1U] >> 2) & 0x0FU));
            bytes[20U] = (byte)((bytes[20U] & 0x03U) | ((DMRSlotType[1U] << 6) & 0xC0U) | ((DMRSlotType[2U] >> 2) & 0x3CU));
        }
    } // public class SlotType
} // namespace dvmdfsi.FNE.DMR
