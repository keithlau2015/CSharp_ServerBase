using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public class NetClient
    {
        private const int DATA_BUFFER_SIZE = 4096;

        public Guid UID { get; private set; }
        private TcpClient tcpClient;
        private byte[] receiveBuffer;

        public bool IsAlive {
            get {
                
                return tcpClient != null &&
                    tcpClient.Connected;
            }
        }
        public NetClient(TcpClient tcpClient)
        {
            UID = Guid.NewGuid();
            this.tcpClient = tcpClient;
            this.receiveBuffer = new byte[DATA_BUFFER_SIZE];
            this.tcpClient.ReceiveBufferSize = DATA_BUFFER_SIZE;
            this.tcpClient.SendBufferSize = DATA_BUFFER_SIZE;
        }

        public async void Send(Packet packet)
        {
            if (!IsAlive)
                return;

            packet.WriteLength();
            try
            {
                await tcpClient.GetStream().WriteAsync(packet.ToBytes(), 0, packet.ToBytes().Length);
            }
            catch (Exception e)
            {
                Debug.DebugUtility.ErrorLog($"SendMsgAsync: {e}");
            }
        }

        public async Task Read()
        {
            if (!IsAlive || tcpClient.Available == 0)
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

                Packet packet = new Packet(receiveData);
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
                await packetHandler.ReadPacket(this, packet);
            }
            catch (Exception e)
            {
                Debug.DebugUtility.ErrorLog($"ReadMsg: {e}");
            }
        }

        public void Disconnect()
        {
            if(Server.netClientMap == null)
                return;

            if(!Server.netClientMap.TryRemove(UID.ToString(), out _))
            {
                Debug.DebugUtility.ErrorLog($"Server.netClientMap {UID.ToString()} not found!");
            }

            if(tcpClient != null)
            {
                tcpClient.GetStream().Close();
                tcpClient.Close();
                tcpClient = null;
            }
            Debug.DebugUtility.DebugLog($"NetClient[{UID.ToString()}] Disconnected");
        }
    }
}