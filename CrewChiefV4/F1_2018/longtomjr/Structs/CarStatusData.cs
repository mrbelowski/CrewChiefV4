// <copyright file="CarStatusData.cs" company="Racing Sim Tools">
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
    /// Car status of a car in the race.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarStatusData
    {
        /// <summary>
        /// 0 (off) - 2 (high)
        /// </summary>
        public byte m_tractionControl;

        /// <summary>
        /// 0 (off) - 1 (on)
        /// </summary>
        public byte m_antiLockBrakes;

        /// <summary>
        /// Fuel mix - 0 = lean, 1 = standard, 2 = rich, 3 = max
        /// </summary>
        public byte m_fuelMix;

        /// <summary>
        /// Front brake bias (percentage)
        /// </summary>
        public byte m_frontBrakeBias;

        /// <summary>
        /// Pit limiter status - 0 = off, 1 = on
        /// </summary>
        public byte m_pitLimiterStatus;

        /// <summary>
        /// Current fuel mass
        /// </summary>
        public float m_fuelInTank;

        /// <summary>
        /// Fuel capacity
        /// </summary>
        public float m_fuelCapacity;

        /// <summary>
        /// Cars max RPM, point of rev limiter
        /// </summary>
        public ushort m_maxRPM;

        /// <summary>
        /// Cars idle RPM
        /// </summary>
        public ushort m_idleRPM;

        /// <summary>
        /// Maximum number of gears
        /// </summary>
        public byte m_maxGears;

        /// <summary>
        /// 0 = not allowed, 1 = allowed, -1 = unknown
        /// </summary>
        public byte m_drsAllowed;

        /// <summary>
        /// Tyre wear percentage
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_tyresWear;

        /// <summary>
        /// Modern - 0 = hyper soft, 1 = ultra soft
        /// 2 = super soft, 3 = soft, 4 = medium, 5 = hard
        /// 6 = super hard, 7 = inter, 8 = wet
        /// Classic - 0-6 = dry, 7-8 = wet
        /// </summary>
        public byte m_tyreCompound;

        /// <summary>
        /// Tyre damage (percentage)
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_tyresDamage;

        /// <summary>
        /// Front left wing damage (percentage)
        /// </summary>
        public byte m_frontLeftWingDamage;

        /// <summary>
        /// Front right wing damage (percentage)
        /// </summary>
        public byte m_frontRightWingDamage;

        /// <summary>
        /// Rear wing damage (percentage)
        /// </summary>
        public byte m_rearWingDamage;

        /// <summary>
        /// Engine damage (percentage)
        /// </summary>
        public byte m_engineDamage;

        /// <summary>
        /// Gear box damage (percentage)
        /// </summary>
        public byte m_gearBoxDamage;

        /// <summary>
        /// Exhaust damage (percentage)
        /// </summary>
        public byte m_exhaustDamage;

        /// <summary>
        /// -1 = invalid/unknown, 0 = none, 1 = green
        /// 2 = blue, 3 = yellow, 4 = red
        /// </summary>
        public sbyte m_vehicleFiaFlags;

        /// <summary>
        /// ERS energy store in Joules
        /// </summary>
        public float m_ersStoreEnergy;

        /// <summary>
        /// ERS deployment mode, 0 = none, 1 = low, 2 = medium
        /// 3 = high, 4 = overtake, 5 = hotlap
        /// </summary>
        public byte m_ersDeployMode;

        /// <summary>
        /// ERS energy harvested this lap by MGU-K
        /// </summary>
        public float m_ersHarvestedThisLapMGUK;

        /// <summary>
        /// ERS energy harvested this lap by MGU-H
        /// </summary>
        public float m_ersHarvestedThisLapMGUH;

        /// <summary>
        /// ERS energy deployed this lap
        /// </summary>
        public float m_ersDeployedThisLap;
    }
}
