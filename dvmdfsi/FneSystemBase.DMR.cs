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
*
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Serilog;

using fnecore;
using fnecore.DMR;

namespace dvmdfsi
{
    /// <summary>
    /// Implements a FNE system base.
    /// </summary>
    public abstract partial class FneSystemBase : fnecore.FneSystemBase
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Callback used to validate incoming DMR data.
        /// </summary>
        /// <param name="peerId">Peer ID</param>
        /// <param name="srcId">Source Address</param>
        /// <param name="dstId">Destination Address</param>
        /// <param name="slot">Slot Number</param>
        /// <param name="callType">Call Type (Group or Private)</param>
        /// <param name="frameType">Frame Type</param>
        /// <param name="dataType">DMR Data Type</param>
        /// <param name="streamId">Stream ID</param>
        /// <param name="message">Raw message data</param>
        /// <returns>True, if data stream is valid, otherwise false.</returns>
        protected override bool DMRDataValidate(uint peerId, uint srcId, uint dstId, byte slot, CallType callType, FrameType frameType, DMRDataType dataType, uint streamId, byte[] message)
        {
            return true;
        }

        /// <summary>
        /// Event handler used to process incoming DMR data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void DMRDataReceived(object sender, DMRDataReceivedEvent e)
        {
            return;
        }
    } // public abstract partial class FneSystemBase : fnecore.FneSystemBase
} // namespace dvmdfsi
