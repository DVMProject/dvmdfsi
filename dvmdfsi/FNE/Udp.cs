﻿/**
* Digital Voice Modem - Fixed Network Equipment
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment
*
*/
/*
*   Copyright (C) 2022 by Bryan Biedenkapp N2PLL
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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace dvmdfsi.FNE
{
    /// <summary>
    /// Structure representing a raw UDP packet frame.
    /// </summary>
    /// <remarks>"Frame" is used loosely here...</remarks>
    public struct UdpFrame
    {
        /// <summary>
        /// 
        /// </summary>
        public IPEndPoint Endpoint;
        /// <summary>
        /// 
        /// </summary>
        public byte[] Message;
    } // public struct UDPFrame

    /// <summary>
    /// Base class from which all UDP classes are derived.
    /// </summary>
    public abstract class UdpBase
    {
        protected UdpClient client;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpBase"/> class.
        /// </summary>
        protected UdpBase()
        {
            client = new UdpClient();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<UdpFrame> Receive()
        {
            UdpReceiveResult res = await client.ReceiveAsync();
            return new UdpFrame()
            {
                Message = res.Buffer,
                Endpoint = res.RemoteEndPoint
            };
        }
    } // public abstract class UDPBase

    /// <summary>
    /// Class implementing a UDP listener (server).
    /// </summary>
    public class UdpListener : UdpBase
    {
        private IPEndPoint listen;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> for this <see cref="UdpListener"/>.
        /// </summary>
        public IPEndPoint EndPoint => listen;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpListener"/> class.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public UdpListener(string address, int port) : this(new IPEndPoint(IPAddress.Parse(address), port))
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpListener"/> class.
        /// </summary>
        /// <param name="endpoint"></param>
        public UdpListener(IPEndPoint endpoint)
        {
            listen = endpoint;
            client = new UdpClient(listen);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        public void Send(UdpFrame frame)
        {
            client.Send(frame.Message, frame.Message.Length, frame.Endpoint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        public async Task<int> SendAsync(UdpFrame frame)
        {
            return await client.SendAsync(frame.Message, frame.Message.Length, frame.Endpoint);
        }
    } // public class UdpListener : UdpBase

    /// <summary>
    /// 
    /// </summary>
    public class UdpReceiver : UdpBase
    {
        private IPEndPoint endpoint;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the <see cref="IPEndPoint"/> for this <see cref="UdpReceiver"/>.
        /// </summary>
        public IPEndPoint EndPoint => endpoint;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpListener"/> class.
        /// </summary>
        public UdpReceiver()
        {
            /* stub */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public void Connect(string hostName, int port)
        {
            try
            {
                try
                {
                    endpoint = new IPEndPoint(IPAddress.Parse(hostName), port);
                }
                catch
                {
                    IPHostEntry entry = Dns.GetHostEntry(hostName);
                    if (entry.AddressList.Length > 0)
                    {
                        IPAddress address = entry.AddressList[0];
                        endpoint = new IPEndPoint(address, port);
                    }
                }
            }
            catch
            {
                return;
            }

            client.Connect(endpoint.Address.ToString(), endpoint.Port); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public void Connect(IPEndPoint endpoint)
        {
            UdpReceiver recv = new UdpReceiver();
            this.endpoint = endpoint;
            client.Connect(endpoint.Address.ToString(), endpoint.Port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame"></param>
        public void Send(UdpFrame frame)
        {
            client.Send(frame.Message, frame.Message.Length);
        }
    } // public class UdpReceiver : UdpBase
} // namespace dvmdfsi.FNE
