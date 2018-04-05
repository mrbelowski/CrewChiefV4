using CrewChiefV4.NumberProcessing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.ACS
{
    public class KMREventListener : EventListener
    {
        private Boolean listening = false;
        private Boolean registered = false;
        public String remoteAddress = "";
        public int remotePort = 0;
        public String appId = "";
        public String apiToken = "";

        private Dictionary<String, String> kmrMessageFragmentToCCMessageFragment = new Dictionary<String, String>();

        private UTF32Encoding encoder = new UTF32Encoding(false, false);

        public KMREventListener()
        {
            // set up kmr message fragment to CC message fragment mappings
            kmrMessageFragmentToCCMessageFragment.Add("", "");
        }

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
            listening = false;
            sendRegisterUDPPacket(this.remoteAddress, this.remotePort, false);
            listening = false;
            this.remoteAddress = "";
            this.remotePort = 0;
            this.apiToken = "";            
            base.deactivate();
        }
        
        private void sendRegisterUDPPacket(String address, int port, Boolean start)
        {
            if ((registered && start) || (!registered && !start))
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
            registered = start;
            if (start && !listening)
            {
                listening = true;
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
                                List<String> decodedData = decodeDataMessage(bytes);
                                
                            }
                        }
                        catch (Exception e)
                        {
                            // timeout or whatever
                            Console.WriteLine("Error: " + e.Message);
                        }
                    }
                }).Start();
            }
        }

        private void playMessageFromReceivedData(List<String> receivedData)
        {
            // only allow the parse to succeed if we have something more than just a number or a timespan
            Boolean gotSomeText = false;
            List<MessageFragment> ccMessageContents = new List<MessageFragment>();
            foreach (String kmrFragment in receivedData)
            {
                String ccFragement;
                if (kmrMessageFragmentToCCMessageFragment.TryGetValue(kmrFragment, out ccFragement))
                {
                    gotSomeText = true;
                    ccMessageContents.Add(MessageFragment.Text(ccFragement));
                }
                else
                {
                    // try and parse this as a number
                    try 
                    {
                        ccMessageContents.Add(MessageFragment.Integer(int.Parse(kmrFragment)));
                    }
                    catch (Exception) 
                    {
                        try 
                        {
                            ccMessageContents.Add(MessageFragment.Time(
                                new TimeSpanWrapper(TimeSpan.Parse(kmrFragment), Precision.AUTO_LAPTIMES)));
                        }
                        catch (Exception) {}
                    }
                }
            }
            if (gotSomeText) 
            {
                audioPlayer.playMessage(new QueuedMessage("KMR_event", ccMessageContents, 0, null));
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
            //Console.WriteLine("Sending data: " + BitConverter.ToString(fullBytes));
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
