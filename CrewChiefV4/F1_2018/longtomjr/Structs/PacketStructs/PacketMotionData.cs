// <copyright file="PacketMotionData.cs" company="Racing Sim Tools">
// Original work Copyright (c) Codemasters. All rights reserved.
//
// Modified work Copyright (c) Racing Sim Tools.
//
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
namespace F1UdpNet
{
    using System.Runtime.InteropServices;

    /// <summary>
    ///  The motion packet gives physics data for all the cars being driven.
    ///  There is additional data for the car being driven with the goal of being
    ///  able to drive a motion platform setup.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketMotionData
    {
        // N.B. For the normalised vectors below, to convert to float values divide by 32767.0f.
        // 16-bit signed values are used to pack the data and on the assumption that
        // direction values are always between -1.0f and 1.0f.

        /// <summary>
        /// Header
        /// </summary>
        public PacketHeader m_header;

        /// <summary>
        /// Data for all cars on track
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
        public CarMotionData[] m_carMotionData;

        // Extra player car ONLY data

        /// <summary>
        /// Position of the suspension at each wheel.
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_suspensionPosition;

        /// <summary>
        /// Velocity of the suspension at each wheel.
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_suspensionVelocity;

        /// <summary>
        /// Acceleration of the suspension at each wheel.
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_suspensionAcceleration;

        /// <summary>
        /// Speed of each wheel.
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_wheelSpeed;

        /// <summary>
        /// Slip ratio for each wheel.
        /// RL, RR, FL, FR
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_wheelSlip;

        /// <summary>
        /// Velocity in local space
        /// </summary>
        public float m_localVelocityX;

        /// <summary>
        /// Velocity in local space
        /// </summary>
        public float m_localVelocityY;

        /// <summary>
        /// Velocity in local space
        /// </summary>
        public float m_localVelocityZ;

        /// <summary>
        /// Angular velocity x-component
        /// </summary>
        public float m_angularVelocityX;

        /// <summary>
        /// Angular velocity y-component
        /// </summary>
        public float m_angularVelocityY;

        /// <summary>
        /// Angular velocity z-component
        /// </summary>
        public float m_angularVelocityZ;

        /// <summary>
        /// Angular velocity x-component
        /// </summary>
        public float m_angularAccelerationX;

        /// <summary>
        /// Angular velocity y-component
        /// </summary>
        public float m_angularAccelerationY;

        /// <summary>
        /// Angular velocity z-component
        /// </summary>
        public float m_angularAccelerationZ;

        /// <summary>
        /// Current front wheels angle in radians
        /// </summary>
        public float m_frontWheelsAngle;

    }
}
