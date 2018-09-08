// <copyright file="CarSetupData.cs" company="Racing Sim Tools">
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
    /// Details of a car's setup.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarSetupData
    {
        /// <summary>
        /// Front wing aero
        /// </summary>
        public byte m_frontWing;

        /// <summary>
        /// Rear wing aero
        /// </summary>
        public byte m_rearWing;

        /// <summary>
        /// Differential adjustment on throttle (percentage)
        /// </summary>
        public byte m_onThrottle;

        /// <summary>
        /// Differential adjustment off throttle (percentage)
        /// </summary>
        public byte m_offThrottle;

        /// <summary>
        /// Front camber angle (suspension geometry)
        /// </summary>
        public float m_frontCamber;

        /// <summary>
        /// Rear camber angle (suspension geometry)
        /// </summary>
        public float m_rearCamber;

        /// <summary>
        /// Front toe angle (suspension geometry)
        /// </summary>
        public float m_frontToe;

        /// <summary>
        /// Rear toe angle (suspension geometry)
        /// </summary>
        public float m_rearToe;

        /// <summary>
        /// Front suspension
        /// </summary>
        public byte m_frontSuspension;

        /// <summary>
        /// Rear suspension
        /// </summary>
        public byte m_rearSuspension;

        /// <summary>
        /// Front anti-roll bar
        /// </summary>
        public byte m_frontAntiRollBar;

        /// <summary>
        /// Front anti-roll bar
        /// </summary>
        public byte m_rearAntiRollBar;

        /// <summary>
        /// Front ride height
        /// </summary>
        public byte m_frontSuspensionHeight;

        /// <summary>
        /// Rear ride height
        /// </summary>
        public byte m_rearSuspensionHeight;

        /// <summary>
        /// Brake pressure (percentage)
        /// </summary>
        public byte m_brakePressure;

        /// <summary>
        /// Brake bias (percentage)
        /// </summary>
        public byte m_brakeBias;

        /// <summary>
        /// Front tyre pressure (PSI)
        /// </summary>
        public float m_frontTyrePressure;

        /// <summary>
        /// Rear tyre pressure (PSI)
        /// </summary>
        public float m_rearTyrePressure;

        /// <summary>
        /// Ballast
        /// </summary>
        public byte m_ballast;

        /// <summary>
        /// Fuel load
        /// </summary>
        public float m_fuelLoad;
    }
}
