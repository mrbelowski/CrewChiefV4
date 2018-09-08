// <copyright file="MarshalZone.cs" company="Racing Sim Tools">
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
    /// Structure of a marshal zone.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MarshalZone
    {
        /// <summary>
        /// Fraction (0..1) of way through the lap the marshal zone starts.
        /// </summary>
        public float m_zoneStart;

        /// <summary>
        /// Flag displayed in the marshal zone.
        /// -1 = invalid/unknown, 0 = none, 1 = green, 2 = blue, 3 = yellow, 4 = red
        /// </summary>
        public byte m_zoneFlag;
    }
}
