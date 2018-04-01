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
            KMREventListener.listening = false;
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, false);
            KMREventListener.listening = false;
            this.remoteAddress = "";
            this.remotePort = 0;
            this.apiToken = "";            
            base.deactivate();
        }

        private void sendRegisterUDPPacket(String address, int port, Boolean start)
        {
            if ((KMREventListener.listening && start) || (!KMREventListener.listening && !start))
            {
                return;
            }
            byte[] contents = getRegisterDatagram(start);
            UdpClient registerUdpClient = new UdpClient(this.remotePort);
            if (this.remoteAddress != null && this.remotePort > 0)
            {
                registerUdpClient.Send(contents, contents.Length, new IPEndPoint(IPAddress.Parse(this.remoteAddress), this.remotePort));
            }
            registerUdpClient.Close();
            if (start)
            {
                KMREventListener.listening = true;
                UdpClient receiveClient = new UdpClient(this.remotePort);
                receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    while (listening)
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                        try
                        {
                            byte[] bytes = receiveClient.Receive(ref ep);
                            byte type = bytes[0];
                            if (type == (byte)2)
                            {
                                List<String> messageFragments = decodeDataMessage(bytes);
                            }
                        }
                        catch (TimeoutException e)
                        {
                            Console.WriteLine("No data received, terminating");
                            deactivate();
                        }
                    }
                    receiveClient.Close();
                }).Start();
            }
        }

        private List<String> decodeDataMessage(byte[] rawData)
        {
            List<String> messageFragments = new List<String>();
            int start = 1;
            while (start < rawData.Length)
            {
                int length = ((int)rawData[start]) * 4;
                messageFragments.Add(encoder.GetString(rawData, start + 1, length));
                start += 1 + length;

            }
            return messageFragments;
        }

        private byte[] getRegisterDatagram(Boolean start)
        {
            // construct from member vars + start / stop flag
            byte[] appIdBytes = encoder.GetBytes(this.appId);
            byte[] apiTokenBytes = encoder.GetBytes(apiToken);

            byte[] fullBytes = new byte[appIdBytes.Length + apiTokenBytes.Length + 3];  // + 3 because 1 byte for each str len + 1 byte for start / stop
            fullBytes[0] = (byte) this.appId.Length;
            Array.Copy(appIdBytes, 0, fullBytes, 1, appIdBytes.Length);
            fullBytes[appIdBytes.Length + 1] = (byte)this.apiToken.Length;
            Array.Copy(apiTokenBytes, 0, fullBytes, appIdBytes.Length + 2, apiTokenBytes.Length);
            fullBytes[fullBytes.Length - 1] = start ? (byte)1 : (byte)0;
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
