// <copyright file="enums.cs" company="Racing Sim Tools">
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
    using System.Text;

    /// <summary>
    /// Id defining the type of a packet.
    /// </summary>
    public enum e_PacketId
    {
        Motion = 0,
        Session = 1,
        LapData = 2,
        Event = 3,
        Participants = 4,
        CarSetups = 5,
        CarTelemetry = 6,
        CarStatus = 7
    }
}
