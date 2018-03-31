using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.ACS
{
    // this is just a placeholder. KMR will provide appID, address, port & API token 
    // either by the user copy/pasting some magic String or via some Python call. CC 
    // will connect to the provided URL and send a 'go' package to register to receive
    // events. Not sure if these are keep-alives sent by the server (as suggested in forum).
    // Also need a "user guid". No idea what this is.
    // As all this shizzle is event based, need to check that a given event only triggers
    // a single message from the remote.
    public class KMREventListener : EventListener
    {
        private static Boolean listening = false;
        public String remoteAddress = "";
        public int remotePort = 0;
        public String appId = "";
        public String apiToken = "";

        private static UdpClient udpClient;

        private Task<UdpReceiveResult> receiveResultTask;

        private UTF32Encoding encoder = new UTF32Encoding(false, false);

        public override void activate(Object activationData)
        {
            if (active)
            {
                deactivate();
            }
            // make the UDP handshake with the remote to register
            KMRHandshakeData handshakeData = (KMRHandshakeData)activationData;
            this.remoteAddress = handshakeData.remoteAddress;
            this.remotePort = handshakeData.remotePort;
            this.apiToken = handshakeData.apiToken;
            this.appId = handshakeData.appId;
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, true);
            base.activate(activationData);
        }

        public override void deactivate()
        {
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, false);
            listening = false;
            this.remoteAddress = "";
            this.remotePort = 0;
            this.apiToken = "";
            base.deactivate();
        }

        private void sendRegisterUDPPacket(String address, int port, Boolean start)
        {
            if ((listening && start) || (!listening && !start))
            {
                Console.WriteLine("Bugger off");
                return;
            }
            byte[] contents = getRegisterDatagram(start);
            udpClient = new UdpClient();
            if (this.remoteAddress != null && this.remotePort > 0)
            {
                udpClient.Send(contents, contents.Length, new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort));
            }
            udpClient.Close();
            if (start)
            {
                listening = true;
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    UdpClient receiver = new UdpClient(this.remotePort);
                    while (listening)
                    {
                        var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        var receivedResults = receiver.Receive(ref remoteEndPoint);
                        Console.WriteLine("Got some data");
                    }
                    receiver.Close();
                }).Start();
            }
        }

        private byte[] getRegisterDatagram(Boolean start)
        {
            // construct from member vars + start / stop flag
            byte[] appIdBytes = encoder.GetBytes(this.appId);
            byte[] apiTokenBytes = encoder.GetBytes(apiToken);

            byte[] fullBytes = new byte[appIdBytes.Length + apiTokenBytes.Length + 3];  // + 3 because 1 byte for each str len + 1 byte for start / stop
            fullBytes[0] = (byte) this.appId.Length;
            Array.Copy(appIdBytes, 0, fullBytes, 1, appIdBytes.Length);
            fullBytes[appIdBytes.Length] = (byte)this.apiToken.Length;
            Array.Copy(apiTokenBytes, 0, fullBytes, appIdBytes.Length + 1, apiTokenBytes.Length);
            fullBytes[fullBytes.Length - 1] = start ? (byte)(uint)1 : (byte)(uint)0;
            return fullBytes;
        }
    }

    public class KMRHandshakeData
    {
        public String remoteAddress;
        public int remotePort;
        public String appId;
        public String apiToken;
    }
}
