// <copyright file="LapData.cs" company="Racing Sim Tools">
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
    /// Lap data for a car.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LapData
    {
        /// <summary>
        /// Last lap time in seconds
        /// </summary>
        public float m_lastLapTime;

        /// <summary>
        /// Current time around the lap in seconds
        /// </summary>
        public float m_currentLapTime;

        /// <summary>
        /// Best lap time of the session in seconds
        /// </summary>
        public float m_bestLapTime;

        /// <summary>
        /// Sector 1 time in seconds
        /// </summary>
        public float m_sector1Time;

        /// <summary>
        /// Sector 2 time in seconds
        /// </summary>
        public float m_sector2Time;

        /// <summary>
        /// Distance vehicle is around current lap in metres – could
        /// be negative if line hasn’t been crossed yet
        /// </summary>
        public float m_lapDistance;

        /// <summary>
        /// Total distance travelled in session in metres – could
        /// be negative if line hasn’t been crossed yet
        /// </summary>
        public float m_totalDistance;

        /// <summary>
        /// Delta in seconds for safety car
        /// </summary>
        public float m_safetyCarDelta;

        /// <summary>
        /// Car race position
        /// </summary>
        public byte m_carPosition;

        /// <summary>
        /// Current lap number
        /// </summary>
        public byte m_currentLapNum;

        /// <summary>
        /// 0 = none, 1 = pitting, 2 = in pit area
        /// </summary>
        public byte m_pitStatus;

        /// <summary>
        /// 0 = sector1, 1 = sector2, 2 = sector3
        /// </summary>
        public byte m_sector;

        /// <summary>
        /// Current lap invalid - 0 = valid, 1 = invalid
        /// </summary>
        public byte m_currentLapInvalid;

        /// <summary>
        /// Accumulated time penalties in seconds to be added
        /// </summary>
        public byte m_penalties;

        /// <summary>
        /// Grid position the vehicle started the race in
        /// </summary>
        public byte m_gridPosition;

        /// <summary>
        /// Status of driver - 0 = in garage, 1 = flying lap
        /// 2 = in lap, 3 = out lap, 4 = on track
        /// </summary>
        public byte m_driverStatus;

        /// <summary>
        /// Result status - 0 = invalid, 1 = inactive, 2 = active
        /// 3 = finished, 4 = disqualified, 5 = not classified
        /// 6 = retired
        /// </summary>
        public byte m_resultStatus;
    }
}
