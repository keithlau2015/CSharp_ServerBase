using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public class NetClient
    {
        public enum SupportProtocol
        {
            TCP,
            UDP,
        }

        public Guid UID { get; private set; }
        private TCProtocol tcProtocol;
        private UDProtocol udProtocol;
        public bool isAlive
        {
            get
            {
                return tcProtocol.isAlive || udProtocol.isAlive;
            }
        }

        public NetClient(IPEndPoint iPEndPoint = null)
        {
            UID = Guid.NewGuid();
            tcProtocol = new TCProtocol(this);
            udProtocol = new UDProtocol(this, iPEndPoint);
        }

        public class TCProtocol
        {
            private NetClient client;
            private const int DATA_BUFFER_SIZE = 4096;
            private TcpClient tcpClient;
            private byte[] receiveBuffer;
            public bool isAlive
            {
                get
                {
                    return tcpClient != null && tcpClient.Connected;
                }
            }
            public TCProtocol(NetClient netClient)
            {
                this.client = netClient;
                this.receiveBuffer = new byte[DATA_BUFFER_SIZE];
                this.tcpClient.ReceiveBufferSize = DATA_BUFFER_SIZE;
                this.tcpClient.SendBufferSize = DATA_BUFFER_SIZE;
            }

            public async void Send(Packet packet)
            {
                if (tcpClient == null || !tcpClient.Connected)
                    return;

                packet.WriteLength();
                try
                {
                    await tcpClient.GetStream().WriteAsync(packet.ToBytes(), 0, packet.ToBytes().Length);
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog($"TCP SendMsgAsync: {e}");
                }
            }

            public async Task Read()
            {
                if (tcpClient == null || !tcpClient.Connected || tcpClient.Available == 0)
                    return;
                try
                {
                    int byteLength = tcpClient.GetStream().Read(receiveBuffer, 0, DATA_BUFFER_SIZE);
                    if (byteLength <= 0)
                        return;

                    byte[] receiveData = new byte[byteLength];
                    Array.Copy(receiveBuffer, receiveData, byteLength);

                    //Clear After Copy from the buffer
                    Array.Clear(receiveBuffer, 0, DATA_BUFFER_SIZE);

                    using (Packet packet = new Packet(receiveData))
                    {
                        //Get Packet Lenght
                        int packetLength = packet.ReadInt();
                        //Get Packet ID
                        string packet_id = packet.ReadString();
                        //Retrieve Packet Handler
                        PacketHandlerBase packetHandler = null;
                        if (!Server.packetHandlers.TryGetValue(packet_id, out packetHandler))
                        {
                            Debug.DebugUtility.ErrorLog($"Invaild Packet ID[{packet_id}]");
                            return;
                        }
                        Debug.DebugUtility.DebugLog($"Received Packet ID[{packet_id}]");
                        await packetHandler.ReadPacket(client, packet);
                    }
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog($"ReadMsg: {e}");
                }
            }

            public void Disconnect()
            {
                if (tcpClient == null)
                    return;
                tcpClient.GetStream().Close();
                tcpClient.Close();
                tcpClient = null;
            }
        }
        public class UDProtocol
        {
            private NetClient client;
            private UdpClient udpClient;
            private IPEndPoint endPoint;
            public bool isAlive
            {
                get
                {
                    return udpClient != null && endPoint != null;
                }
            }
            public UDProtocol(NetClient netClient, IPEndPoint iPEndPoint)
            {
                this.client = netClient;
                this.endPoint = iPEndPoint;
            }

            public void SetIPEndPoint(IPEndPoint iPEndPoint)
            {
                this.endPoint = iPEndPoint;
            }

            public async void Send(Packet packet)
            {
                if (udpClient == null || endPoint == null)
                    return;
                packet.WriteLength();
                try
                {
                    if (udpClient != null)
                    {
                        await udpClient.SendAsync(packet.ToBytes(), packet.Length());
                    }
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog($"UDP SendMsgAsync: {e}");
                }
            }

            public async Task Read()
            {
                if (udpClient == null || endPoint == null || udpClient.Available == 0)
                    return;
                try
                {
                    UdpReceiveResult udpReceiveResult = await udpClient.ReceiveAsync();
                    if (udpReceiveResult.Buffer.Length < 4)
                    {
                        return;
                    }

                    using (Packet packet = new Packet(udpReceiveResult.Buffer))
                    {
                        //Get Packet Lenght
                        int packetLength = packet.ReadInt();
                        //Get Packet ID
                        string packet_id = packet.ReadString();
                        //Retrieve Packet Handler
                        PacketHandlerBase packetHandler = null;
                        if (!Server.packetHandlers.TryGetValue(packet_id, out packetHandler))
                        {
                            Debug.DebugUtility.ErrorLog($"Invaild Packet ID[{packet_id}]");
                            return;
                        }
                        Debug.DebugUtility.DebugLog($"Received Packet ID[{packet_id}]");
                        await packetHandler.ReadPacket(client, packet);
                    }
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog($"ReadMsg: {e}");
                }
            }

            public void Disconnect()
            {
                udpClient.Close();
                endPoint = null;
                udpClient = null;
            }
        }
        public void Send(Packet packet, SupportProtocol supportProtocol = SupportProtocol.TCP)
        {
            if (supportProtocol.Equals(SupportProtocol.TCP))
                tcProtocol.Send(packet);
            else if(supportProtocol.Equals(SupportProtocol.UDP))
                udProtocol.Send(packet);
        }

        public async void Read(SupportProtocol supportProtocol = SupportProtocol.TCP)
        {
            if (supportProtocol.Equals(SupportProtocol.TCP))
                await tcProtocol.Read();
            else if( supportProtocol.Equals(SupportProtocol.UDP))
                await udProtocol.Read();
        }
        public void Disconnect()
        {
            if(Server.netClientMap == null)
                return;

            if(!Server.netClientMap.TryRemove(UID.ToString(), out _))
            {
                Debug.DebugUtility.ErrorLog($"Server.netClientMap {UID.ToString()} not found!");
            }

            if(tcProtocol != null)
            {
                tcProtocol.Disconnect();
            }

            if(udProtocol != null)
            {
                udProtocol.Disconnect();
            }
            Debug.DebugUtility.DebugLog($"NetClient[{UID.ToString()}] Disconnected");
        }
    }
}