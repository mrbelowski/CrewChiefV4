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
    public struct LapSector
    {
        public readonly int LapNumber;
        public readonly int Sector;

        public LapSector(int lapNumber, int sector)
        {
            Sector = sector;
            LapNumber = lapNumber;
        }

        public static LapSector ForLap(int lapNumber)
        {
            return new LapSector(lapNumber, 0);
        }

        public override bool Equals(Object obj)
        {
            return obj is LapSector && this == (LapSector)obj;
        }

        public override int GetHashCode()
        {
            return LapNumber << 4 + Sector;
        }
        
        public static bool operator ==(LapSector x, LapSector y)
        {
            return x.LapNumber == y.LapNumber && x.Sector == y.Sector;
        }

        public static bool operator !=(LapSector x, LapSector y)
        {
            return !(x == y);
        }

        public static bool operator >=(LapSector x, LapSector y)
        {
            if (x.LapNumber > y.LapNumber)
                return true;

            if (x.LapNumber == y.LapNumber && x.Sector >= y.Sector)
                return true;

            return false;
        }

        public static bool operator <=(LapSector x, LapSector y)
        {
            return y >= x;
        }

        public override string ToString()
        {
            return string.Format("Lap: {0}, Sector: {1}", LapNumber, Sector);
        }
    }
}
