// <copyright file="CarMotionData.cs" company="Racing Sim Tools">
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
    using System.Runtime.InteropServices;

    /// <summary>
    /// Motion data of all the cars.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarMotionData
    {
        // N.B. For the normalised vectors below, to convert to float values divide by 32767.0f.
        // 16-bit signed values are used to pack the data and on the assumption that
        // direction values are always between -1.0f and 1.0f.

        /// <summary>
        /// World space X position
        /// </summary>
        public float m_worldPositionX;

        /// <summary>
        /// World space Y position
        /// </summary>
        public float m_worldPositionY;

        /// <summary>
        /// World space Z position
        /// </summary>
        public float m_worldPositionZ;

        /// <summary>
        /// Velocity in world space X
        /// </summary>
        public float m_worldVelocityX;

        /// <summary>
        /// Velocity in world space Y
        /// </summary>
        public float m_worldVelocityY;

        /// <summary>
        /// Velocity in world space Z
        /// </summary>
        public float m_worldVelocityZ;

        /// <summary>
        /// World space forward X direction (normalised)
        /// </summary>
        public short m_worldForwardDirX;

        /// <summary>
        /// World space forward Y direction (normalised)
        /// </summary>
        public short m_worldForwardDirY;

        /// <summary>
        /// World space forward Z direction (normalised)
        /// </summary>
        public short m_worldForwardDirZ;

        /// <summary>
        /// World space right X direction (normalised)
        /// </summary>
        public short m_worldRightDirX;

        /// <summary>
        /// World space right Y direction (normalised)
        /// </summary>
        public short m_worldRightDirY;

        /// <summary>
        /// World space right Z direction (normalised)
        /// </summary>
        public short m_worldRightDirZ;

        /// <summary>
        /// Lateral G-Force component
        /// </summary>
        public float m_gForceLateral;

        /// <summary>
        /// Longitudinal G-Force component
        /// </summary>
        public float m_gForceLongitudinal;

        /// <summary>
        /// Vertical G-Force component
        /// </summary>
        public float m_gForceVertical;

        /// <summary>
        /// Yaw angle in radians
        /// </summary>
        public float m_yaw;

        /// <summary>
        /// Pitch angle in radians
        /// </summary>
        public float m_pitch;

        /// <summary>
        /// Roll angle in radians
        /// </summary>
        public float m_roll;
    }
}
