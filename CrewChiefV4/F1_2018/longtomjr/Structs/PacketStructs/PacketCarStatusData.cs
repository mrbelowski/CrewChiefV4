// <copyright file="PacketCarStatusData.cs" company="Racing Sim Tools">
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
    /// This packet details car statuses for all the cars in the race.
    /// It includes values such as the damage readings on the car.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketCarStatusData
    {
        /// <summary>
        /// Header
        /// </summary>
        public PacketHeader m_header;

        /// <summary>
        /// Car status data for every car in the session.
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
        public CarStatusData[] m_carStatusData;

    }
}
