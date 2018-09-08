// <copyright file="PacketSessionData.cs" company="Racing Sim Tools">
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
    /// The session packet includes details about the current session in progress
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketSessionData
    {
        /// <summary>
        /// Header
        /// </summary>
        public PacketHeader m_header;

        /// <summary>
        /// Weather - 0 = clear, 1 = light cloud, 2 = overcast
        /// 3 = light rain, 4 = heavy rain, 5 = storm
        /// </summary>
        public byte m_weather;

        /// <summary>
        /// Track temp. in degrees celsius
        /// </summary>
        public sbyte m_trackTemperature;

        /// <summary>
        /// Air temp. in degrees celsius
        /// </summary>
        public sbyte m_airTemperature;

        /// <summary>
        /// Total number of laps in this race
        /// </summary>
        public byte m_totalLaps;

        /// <summary>
        /// Track length in metres
        /// </summary>
        public ushort m_trackLength;

        /// <summary>
        /// 0 = unknown, 1 = P1, 2 = P2, 3 = P3, 4 = Short P
        /// 5 = Q1, 6 = Q2, 7 = Q3, 8 = Short Q, 9 = OSQ
        /// 10 = R, 11 = R2, 12 = Time Trial
        /// </summary>
        public byte m_sessionType;

        /// <summary>
        /// -1 for unknown, 0-21 for tracks, see appendix
        /// </summary>
        public sbyte m_trackId;

        /// <summary>
        /// Era, 0 = modern, 1 = classic
        /// </summary>
        public byte m_era;

        /// <summary>
        /// Time left in session in seconds
        /// </summary>
        public ushort m_sessionTimeLeft;

        /// <summary>
        /// Session duration in seconds
        /// </summary>
        public ushort m_sessionDuration;

        /// <summary>
        /// Pit speed limit in kilometres per hour
        /// </summary>
        public byte m_pitSpeedLimit;

        /// <summary>
        /// Whether the game is paused
        /// </summary>
        public byte m_gamePaused;

        /// <summary>
        /// Whether the player is spectating
        /// </summary>
        public byte m_isSpectating;

        /// <summary>
        /// Index of the car being spectated
        /// </summary>
        public byte m_spectatorCarIndex;

        /// <summary>
        /// SLI Pro support, 0 = inactive, 1 = active
        /// </summary>
        public byte m_sliProNativeSupport;

        /// <summary>
        /// Number of marshal zones to follow
        /// </summary>
        public byte m_numMarshalZones;

        /// <summary>
        /// List of marshal zones – max 21
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 21)]
        public MarshalZone[] m_marshalZones;

        /// <summary>
        /// 0 = no safety car, 1 = full safety car
        /// 2 = virtual safety car
        /// </summary>
        public byte m_safetyCarStatus;

        /// <summary>
        /// 0 = offline, 1 = online
        /// </summary>
        public byte m_networkGame;

    }
}
