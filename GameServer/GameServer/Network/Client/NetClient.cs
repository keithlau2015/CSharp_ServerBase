using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Network
{
    public class NetClient
    {
        public Guid UID { get; private set; }
        private TcpClient tcpClient;
        public bool IsAlive { 
            get {
                
                return tcpClient != null 
                    && tcpClient.Connected;
            }
        }
        public NetClient(TcpClient tcpClient)
        {
            UID = Guid.NewGuid();
            this.tcpClient = tcpClient;
        }

        public async void Send(Packet packet)
        {
            if (!IsAlive)
                return;

            using (packet)
            {
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
        }

        public async Task Read()
        {
            if (!IsAlive)
                return;

            byte[] receivedBytes = new byte[tcpClient.ReceiveBufferSize];
            try
            {
                await tcpClient.GetStream().ReadAsync(receivedBytes, 0, tcpClient.ReceiveBufferSize);
                if (!tcpClient.GetStream().DataAvailable)
                    return;

                using (Packet packet = new Packet(receivedBytes))
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
                    await packetHandler.ReadPacket(this, packet);
                }
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