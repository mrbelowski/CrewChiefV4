// <copyright file="PacketHeader.cs" company="Racing Sim Tools">
// Original work Copyright (c) Codemasters. All rights reserved.
//
// Modified work Copyright (c) Racing Sim Tools.
//
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>

namespace F1UdpNet
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Header of every packet.
    /// </summary>
    [StructLayout(LayoutKind.Sequential,Pack=1)]
    public struct PacketHeader
    {
        /// <summary>
        /// The year of the game - represents the format of the packets.
        /// </summary>
        public ushort m_packetFormat;

        /// <summary>
        /// Version of this packet type, all start from 1.
        /// </summary>
        public byte m_packetVersion;

        /// <summary>
        /// Identifier for the packet type.
        /// See <see cref="e_PacketId"/>.
        /// </summary>
        public byte m_packetId;

        /// <summary>
        /// Unique identifier for the session.
        /// </summary>
        public ulong m_sessionUID;

        /// <summary>
        /// Session timestamp.
        /// [Unit = Seconds]
        /// TODO: Confirm unit.
        /// </summary>
        public float m_sessionTime;

        /// <summary>
        /// Identifier of the frame the packet was retrieved from.
        /// </summary>
        public uint m_frameIdentifier;

        /// <summary>
        /// Index of the player's car in the array.
        /// </summary>
        public byte m_playerCarIndex;
    }
}
