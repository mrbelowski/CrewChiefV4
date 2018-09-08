// <copyright file="PacketParticipantsData.cs" company="Racing Sim Tools">
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
    /// This is a list of participants in the race. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketParticipantsData
    {
        /// <summary>
        /// Header
        /// </summary>
        public PacketHeader m_header;

        /// <summary>
        /// Number of cars in the data
        /// </summary>
        public byte m_numCars;

        /// <summary>
        /// Participant data for every car
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
        public ParticipantData[] m_participants;

    }
}
