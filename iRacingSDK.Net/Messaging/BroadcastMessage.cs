// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace iRacingSDK
{
    /// <summary>
    /// Remote controll the sim by sending these windows messages
    /// camera and replay commands only work when you are out of your car, 
    /// pit commands only work when in your car
    /// </summary>
    public enum BroadcastMessage
    {
        /// <summary>
        /// The camera switch position : car position, group, camera
        /// </summary>
        CameraSwitchPos = 0,

        /// <summary>
        /// The camera switch number : driver #, group, camera
        /// </summary>
        CameraSwitchNum,

        /// <summary>
        /// The state of the camera set: CameraState, unused, unused
        /// </summary>
        CameraSetState,

        /// <summary>
        /// The replay set play speed: speed, slowMotion, unused
        /// </summary>
        ReplaySetPlaySpeed,

        /// <summary>
        /// The replay set play position: ReplayPositionMode, Frame Number (high, low)
        /// </summary>
        ReplaySetPlayPosition,

        /// <summary>
        /// The replay search : ReplaySearchMode, unused, unused
        /// </summary>
        ReplaySearch,

        /// <summary>
        /// The state of the replay set : ReplayStateMode, unused, unused
        /// </summary>
        ReplaySetState,

        /// <summary>
        /// The reload textures : ReloadTexturesMode, carIdx, unused
        /// </summary>
        ReloadTextures,

        /// <summary>
        /// The chat comand : ChatCommandMode, subCommand, unused
        /// </summary>
        ChatComand,

        /// <summary>
        /// The pit command : PitCommandMode, parameter
        /// this only works when the driver is in the car
        /// </summary>
        PitCommand,

        /// <summary>
        /// The Telemetry Command : TelemCommandMode, ...
        /// You can call this any time, but telemtry only records when driver is in there car
        /// </summary>
        BroadcastTelemetryCommand,

        /// <summary>
        /// value (float, high, low)
        /// </summary>
        BroadcastFFBCommand,
        
        /// <summary>
        /// sessionTimeMS (high, low)
        /// </summary>
        BroadcastReplaySearchSessionTime
    };
}
