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

using iRacingSDK.Support;
using System;
using System.Linq;
using System.Threading;

namespace iRacingSDK
{
    public class NoWaitReplay : Replay
    {
        public NoWaitReplay(iRacingConnection iRacingInstance)
            : base(iRacingInstance)
        {
        }

        protected override void SendMessage(BroadcastMessage message, short var1 = 0, int var2 = 0)
        {
            var msgVar1 = FromShorts((short)message, var1);

            if (!Win32.Messages.SendNotifyMessage(Win32.Messages.HWND_BROADCAST, messageId, msgVar1, var2))
                throw new Exception(String.Format("Error in broadcasting message {0}", message));
        }

        public override void Wait()
        {
        }
    }

    public class Replay : iRacingMessaging
    {
        iRacingConnection iRacingInstance;

        public Replay(iRacingConnection iRacingInstance)
        {
            this.iRacingInstance = iRacingInstance;
        }

        public NoWaitReplay NoWait
        {
            get
            {
                return new NoWaitReplay(iRacingInstance);
            }
        }
        public Func<T, T2> WaitOn<T, T2>(Action action, Func<T, T2> testFn)
        {
            return x => testFn(x);
        }

        public void SetSpeed(int p)
        {
            TraceInfo.WriteLine("Setting speed to {0}", p);
            SendMessage(BroadcastMessage.ReplaySetPlaySpeed, (short)p, 0);
        }

        public void MoveToStart()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.ToStart);

            WaitAndVerify(data => data.Telemetry.ReplayFrameNum > 100);
        }

        public void MoveToEnd()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.ToEnd);

            Wait();
        }

        public void MoveToNextSession()
        {
            Wait();

            var data = iRacing.GetDataFeed().First();
            if (data.Telemetry.SessionNum == data.SessionData.SessionInfo.Sessions.Length)
                return;

            ReplaySearch(ReplaySearchMode.NextSession);

            WaitAndVerify(data2 => data.Telemetry.SessionNum + 1 != data2.Telemetry.SessionNum);
        }

        public void MoveToFrame(int frameNumber, ReplayPositionMode mode = ReplayPositionMode.Begin, int tolerance = 32)
        {
            DataSample data = null;

            Wait();

            TraceInfo.WriteLine("Moving to frame {0} with mode {1}", frameNumber, mode);

            SendMessage(BroadcastMessage.ReplaySetPlayPosition, (short)mode, frameNumber);

            Wait();

            if (mode == ReplayPositionMode.Begin)
                data = WaitAndVerify(d => Math.Abs(d.Telemetry.ReplayFrameNum - frameNumber) > tolerance);

            Wait();

            if (data != null)
                frameNumber = data.Telemetry.ReplayFrameNum;

            TraceInfo.WriteLine("Moved to frame {0}", frameNumber);
        }

        public void MoveToStartOfRace()
        {
            MoveToStart();
            var data = iRacing.GetDataFeed().First();

            var session = data.SessionData.SessionInfo.Sessions.FirstOrDefault(s => s.SessionType == "Race");
            if (session == null)
                throw new Exception("No race session found in this replay");

            WaitAndVerify(d => d.Telemetry.SessionNum != session.SessionNum, () => MoveToNextSession());
        }

        public void MoveToQualifying()
        {
            MoveToStart();
            var data = iRacing.GetDataFeed().First();

            var session = data.SessionData.SessionInfo.Sessions.FirstOrDefault(s => s.SessionType.ToLower().Contains("qualify"));
            if (session == null)
                throw new Exception("No qualifying session found in this replay");

            WaitAndVerify(d => d.Telemetry.SessionNum != session.SessionNum, () => MoveToNextSession());
        }

        public bool AttempToMoveToQualifyingSection()
        {
            try
            {
                MoveToQualifying();
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }


        public void MoveToParadeLap()
        {
            MoveToStartOfRace();

            foreach (var data in iRacing.GetDataFeed())
            {
                if (data.Telemetry.SessionState == SessionState.Racing)
                    break;

                this.SetSpeed(16);
            }

            this.SetSpeed(0);
        }

        /// <summary>
        /// Select the camera onto a car and position to 4 seconds before the incident
        /// </summary>
        public void MoveToNextIncident()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.NextIncident);

            Wait();
        }

        /// <summary>
        /// Select the camera onto a car and position to a previous incident marker
        /// </summary>
        public void MoveToPrevIncident()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.PrevIncident);

            Wait();
        }

        public void MoveToNextLap()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.NextLap);

            Wait();
        }

        public void MoveToNextFrame()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.NextFrame);

            Wait();
        }

        public void MoveToPrevFrame()
        {
            Wait();

            ReplaySearch(ReplaySearchMode.PrevFrame);

            Wait();
        }

        public void CameraOnDriver(short carNumber, short group, short camera = 0)
        {
            SendMessage(BroadcastMessage.CameraSwitchNum, carNumber, group, camera);
        }

        public void CameraOnPositon(short carPosition, short group, short camera = 0)
        {
            SendMessage(BroadcastMessage.CameraSwitchPos, carPosition, group, camera);
        }

        void ReplaySearch(ReplaySearchMode mode)
        {
            TraceDebug.WriteLine("Replay Search {0}", mode.ToString());

            SendMessage(BroadcastMessage.ReplaySearch, (short)mode);
        }

        DataSample WaitAndVerify(Func<DataSample, bool> verifyFn)
        {
            return WaitAndVerify(verifyFn, () => { });
        }

        DataSample WaitAndVerify(Func<DataSample, bool> verifyFn, Action action)
        {
            const int wait = 60000;

            if (iRacingInstance.IsRunning)
                return null;

            var timeout = DateTime.Now + TimeSpan.FromMilliseconds(wait);
            var data = iRacing.GetDataFeed().First();
            while ((!data.IsConnected || verifyFn(data)) && DateTime.Now < timeout)
            {
                action();
                data = iRacing.GetDataFeed().First();
                Thread.Sleep(100);
            }

            if (verifyFn(data))
                throw new Exception("iRacing failed to respond to message");

            return data;
        }
    }
}
