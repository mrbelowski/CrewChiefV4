// <copyright file="PacketCarTelemetryData.cs" company="Racing Sim Tools">
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
    /// This packet details telemetry for all the cars in the race.
    /// It details various values that would be recorded on the car such as speed, 
    /// throttle application, DRS etc.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketCarTelemetryData
    {
        /// <summary>
        /// Header
        /// </summary>
        public PacketHeader m_header;

        /// <summary>
        /// Telemetry data for every car
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
        public CarTelemetryData[] m_carTelemetryData;

        /// <summary>
        /// Bit flags specifying which buttons are being
        /// pressed currently - see appendices
        /// </summary>
        public byte m_buttonStatus1;
        public byte m_buttonStatus2;
        public byte m_buttonStatus3;
        public byte m_buttonStatus4;

    }
}
