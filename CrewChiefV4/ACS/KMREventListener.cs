using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        private static UdpClient udpListenClient;

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
            UdpClient client = new UdpClient();
            if (this.remoteAddress != null && this.remotePort > 0)
            {
                client.Send(contents, contents.Length, new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort));
                client.Close();
            }

            if (this.receiveResultTask != null)
            {
                // proper way to cancel this task?
                receiveResultTask.Dispose();
            }
            if (udpListenClient != null)
            {
                udpListenClient.Close();
            }

            if (start) {
                // initialise the receiving socket
                udpListenClient = new UdpClient();
                IPEndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort);
                udpListenClient.Connect(remoteAddress);
                listening = true;
                Task.Run(async () =>
                {
                    while (listening)
                    {
                        //IPEndPoint object will allow us to read datagrams sent from any source.
                        var receivedResults = await KMREventListener.udpListenClient.ReceiveAsync();
                        Console.WriteLine("Got some data");
                    }
                });
            }
        }

        private byte[] getRegisterDatagram(Boolean start)
        {
            // construct from member vars + start / stop flag
            String fullRegisterStr = this.appId + this.apiToken;
            byte[] registerBytes = encoder.GetBytes(fullRegisterStr);
            byte[] registerDatagramContent = new byte[registerBytes.Length + 1];
            Array.Copy(registerBytes, registerDatagramContent, registerBytes.Length);
            registerDatagramContent[registerDatagramContent.Length - 1] = start ? (byte)(uint)1 : (byte)(uint)0;
            return registerDatagramContent;
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
