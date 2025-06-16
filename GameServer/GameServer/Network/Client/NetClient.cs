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
        public TCProtocol tcProtocol { get; private set; }
        public UDProtocol udProtocol { get; private set; }
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
            }

            public void SetUp(TcpClient tcpClient)
            {
                this.tcpClient = tcpClient;
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
                if (tcpClient == null || !tcpClient.Connected)
                    return;

                try
                {
                    NetworkStream stream = tcpClient.GetStream();
                    if (!stream.CanRead || !stream.DataAvailable)
                        return;

                    // Use ReadAsync instead of Read for better async handling
                    int byteLength = await stream.ReadAsync(receiveBuffer, 0, DATA_BUFFER_SIZE);
                    if (byteLength <= 0)
                        return;

                    byte[] receiveData = new byte[byteLength];
                    Array.Copy(receiveBuffer, receiveData, byteLength);

                    //Clear After Copy from the buffer
                    Array.Clear(receiveBuffer, 0, DATA_BUFFER_SIZE);

                    using (Packet packet = new Packet(receiveData))
                    {
                        // Validate packet has minimum required data
                        if (packet.UnreadLength() < 4)
                        {
                            Debug.DebugUtility.WarningLog("Received packet too small to contain length");
                            return;
                        }

                        //Get Packet Length
                        int packetLength = packet.ReadInt();
                        
                        // Validate packet length
                        if (packetLength <= 0 || packetLength > DATA_BUFFER_SIZE)
                        {
                            Debug.DebugUtility.ErrorLog($"Invalid packet length: {packetLength}");
                            return;
                        }

                        // Check if we have enough data for packet ID
                        if (packet.UnreadLength() < 4) // Assuming string length is at least 4 bytes
                        {
                            Debug.DebugUtility.WarningLog("Packet too small to contain packet ID");
                            return;
                        }

                        //Get Packet ID
                        string packet_id = packet.ReadString();
                        
                        if (string.IsNullOrEmpty(packet_id))
                        {
                            Debug.DebugUtility.ErrorLog("Received packet with empty or null packet ID");
                            return;
                        }

                        //Retrieve Packet Handler
                        PacketHandlerBase packetHandler = null;
                        if (!Server.packetHandlers.TryGetValue(packet_id, out packetHandler))
                        {
                            Debug.DebugUtility.ErrorLog($"Invalid Packet ID[{packet_id}]");
                            return;
                        }
                        
                        Debug.DebugUtility.DebugLog($"Received Packet ID[{packet_id}]");
                        await packetHandler.ReadPacket(client, packet);
                    }
                }
                catch (System.IO.IOException ioEx)
                {
                    Debug.DebugUtility.WarningLog($"Network I/O error (client likely disconnected): {ioEx.Message}");
                    // Mark client for disconnection
                    tcpClient?.Close();
                }
                catch (ObjectDisposedException)
                {
                    // Client already disposed, ignore
                    Debug.DebugUtility.DebugLog("TCP client already disposed");
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"TCP Read error: {ex.Message}");
                    // Don't rethrow, just log and continue
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