// <copyright file="ParticipantData.cs" company="Racing Sim Tools">
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
    /// <para>If the vehicle is controlled by AI, then the name will be the driver name.</para>
    ///
    /// <para>
    /// If this is a multiplayer game, the names will be the Steam Id on PC,
    /// or the LAN name if appropriate.
    /// </para>
    ///
    /// <para>On Xbox One, the names will always be the driver name.</para>
    ///
    /// <para>
    /// On PS4 the name will be the LAN name if playing a LAN game,
    /// otherwise it will be the driver name.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParticipantData
    {
        /// <summary>
        /// Whether the vehicle is AI (1) or Human (0) controlled
        /// </summary>
        public byte m_aiControlled;

        /// <summary>
        /// Driver id - see appendix
        /// </summary>
        public byte m_driverId;

        /// <summary>
        /// Team id - see appendix
        /// </summary>
        public byte m_teamId;

        /// <summary>
        /// Race number of the car
        /// </summary>
        public byte m_raceNumber;

        /// <summary>
        /// Nationality of the driver
        /// </summary>
        public byte m_nationality;

        /// <summary>
        /// Name of participant in UTF-8 format – null terminated
        /// Will be truncated with … (U+2026) if too long
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] m_name;
    }
}
