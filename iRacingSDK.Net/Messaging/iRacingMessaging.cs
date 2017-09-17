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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace iRacingSDK
{
    public class iRacingMessaging
    {
        protected readonly int messageId;
        DateTime lastMessagePostedTime = DateTime.Now;
        Task currentMessageTask;
        const double MessageThrottleTime = 1000;

        protected iRacingMessaging()
        {
            messageId = Win32.Messages.RegisterWindowMessage("IRSDK_BROADCASTMSG");
            currentMessageTask = new Task(() => { });
            currentMessageTask.Start();
        }

        protected virtual void SendMessage(BroadcastMessage message, short var1 = 0, int var2 = 0)
        {
            var msgVar1 = FromShorts((short)message, var1);

            var lastTask = currentMessageTask;
            currentMessageTask = new Task(() =>
            {
                lastTask.Wait();
                lastTask.Dispose();
                lastTask = null;

                var timeSinceLastMsg = DateTime.Now - lastMessagePostedTime;
                var throttleTime = (int)(MessageThrottleTime - timeSinceLastMsg.TotalMilliseconds);
                if (throttleTime > 0)
                {
                    Trace.WriteLine(string.Format("Throttle message {0} delivery to iRacing by {1} millisecond", message, throttleTime), "DEBUG");
                    Thread.Sleep(throttleTime);
                }
                lastMessagePostedTime = DateTime.Now;

                if (!Win32.Messages.SendNotifyMessage(Win32.Messages.HWND_BROADCAST, messageId, msgVar1, var2))
                    throw new Exception(String.Format("Error in broadcasting message {0}", message));
            });

            currentMessageTask.Start();
        }

        protected void SendMessage(BroadcastMessage message, short var1, short var2, short var3)
        {
            var var23 = FromShorts(var2, var3);
            SendMessage(message, var1, var23);
        }
        
        protected static int FromShorts(short lowPart, short highPart)
        {
            return ((int)highPart << 16) | (ushort)lowPart;
        }

        public virtual void Wait()
        {
            currentMessageTask.Wait();
        }
    }
}
