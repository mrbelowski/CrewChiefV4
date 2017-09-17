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


namespace iRacingSDK
{
    public class PitCommand : iRacingMessaging
    {
        void SendMessage(PitCommandMode command, int var2 = 0)
        {
            SendMessage(BroadcastMessage.PitCommand, (short)command, var2);
        }

        public void Clear()
        {
            SendMessage(PitCommandMode.Clear);
        }

        public void CleanWindshield()
        {
            SendMessage(PitCommandMode.Windshield);

        }

        public void SetFuel(int amount = 0)
        {
            SendMessage(PitCommandMode.Fuel, amount);
        }

        public void ChangeLeftFrontTire(int kpa = 0)
        {
            SendMessage(PitCommandMode.LeftFront, kpa);
        }

        public void ChangeRightFrontTire(int kpa = 0)
        {
            SendMessage(PitCommandMode.RightFront, kpa);
        }

        public void ChangeLeftRearTire(int kpa = 0)
        {
            SendMessage(PitCommandMode.LeftRear, kpa);
        }

        public void ChangeRightRearTire(int kpa = 0)
        {
            SendMessage(PitCommandMode.RightRear, kpa);
        }

        public void ClearTireChange()
        {
            SendMessage(PitCommandMode.ClearTires);
        }

        public void ChangeAllTyres()
        {
            ChangeLeftFrontTire();
            ChangeRightFrontTire();
            ChangeLeftRearTire();
            ChangeRightRearTire();
        }

        public void RequestFastRepair()
        {
            SendMessage(PitCommandMode.FastRepair);
        }
    }
}
