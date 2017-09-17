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
    public enum PitCommandMode
    {
        /// <summary>
        /// Clear all pit checkboxes
        /// </summary>
        Clear = 0,
        /// <summary>
        /// Clean the winshield, using one tear off
        /// </summary>
        Windshield,
        /// <summary>
        /// Add fuel, optionally specify the amount to add in liters or pass '0' to use existing amount
        /// </summary>
        Fuel,
        /// <summary>
        /// Change the left front tire, optionally specifying the pressure in KPa or pass '0' to use existing pressure
        /// </summary>
        LeftFront,
        /// <summary>
        /// Change the right front tire, optionally specifying the pressure in KPa or pass '0' to use existing pressure
        /// </summary>
        RightFront,
        /// <summary>
        /// Change the left rear tire, optionally specifying the pressure in KPa or pass '0' to use existing pressure
        /// </summary>
        LeftRear,
        /// <summary>
        /// Change the right rear tire, optionally specifying the pressure in KPa or pass '0' to use existing pressure
        /// </summary>
        RightRear,
        /// <summary>
        /// Clear tire pit checkboxes tire, optionally specifying the pressure in KPa or pass '0' to use existing pressure
        /// </summary>
        ClearTires,
        /// <summary>
        /// Request fast repair
        /// </summary>
        FastRepair
    };
}
