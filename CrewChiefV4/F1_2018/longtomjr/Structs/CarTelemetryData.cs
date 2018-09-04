// <copyright file="CarTelemetryData.cs" company="Racing Sim Tools">
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
    /// Telemetry of a car.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarTelemetryData
    {
        /// <summary>
        /// Speed of car in kilometres per hour
        /// </summary>
        public ushort m_speed;

        /// <summary>
        /// Amount of throttle applied (0 to 100)
        /// </summary>
        public byte m_throttle;

        /// <summary>
        /// Steering (-100 (full lock left) to 100 (full lock right))
        /// </summary>
        public sbyte m_steer;

        /// <summary>
        /// Amount of brake applied (0 to 100)
        /// </summary>
        public byte m_brake;

        /// <summary>
        /// Amount of clutch applied (0 to 100)
        /// </summary>
        public byte m_clutch;

        /// <summary>
        /// Gear selected (1-8, N=0, R=-1)
        /// </summary>
        public sbyte m_gear;

        /// <summary>
        /// Engine RPM
        /// </summary>
        public ushort m_engineRPM;

        /// <summary>
        /// 0 = off, 1 = on
        /// </summary>
        public byte m_drs;

        /// <summary>
        /// Rev lights indicator (percentage)
        /// </summary>
        public byte m_revLightsPercent;

        /// <summary>
        /// Brakes temperature (celsius)
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] m_brakesTemperature;

        /// <summary>
        /// Tyres surface temperature (celsius)
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] m_tyresSurfaceTemperature;

        /// <summary>
        /// Tyres inner temperature (celsius)
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] m_tyresInnerTemperature;

        /// <summary>
        /// Engine temperature (celsius)
        /// </summary>
        public ushort m_engineTemperature;

        /// <summary>
        /// Tyres pressure (PSI)
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_tyresPressure;
    }
}
