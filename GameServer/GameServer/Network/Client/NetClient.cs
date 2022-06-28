using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public class NetClient
    {    
        public Guid guid { get; private set; }
        private TcpClient tcpClient;
        public bool isAlive { get; private set;}
        public NetClient(TcpClient tcpClient)
        {
            guid = Guid.NewGuid();
            this.tcpClient = tcpClient;
        }

        ~NetClient()
        {
            if (tcpClient.Connected)
                tcpClient.Close();
            tcpClient.Dispose();
        }

        public async void Send(Packet packet)
        {
            packet.WriteLength();
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.WriteAsync(packet.ToBytes(), 0, packet.ToBytes().Length);
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"SendMsgAsync: {e}");
                }
            }
        }

        public async Task Read()
        {
            byte[] receivedBytes = new byte[tcpClient.ReceiveBufferSize];
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.ReadAsync(receivedBytes, 0, tcpClient.ReceiveBufferSize);
                    using(Packet packet = new Packet(receivedBytes))
                    {
                        //Get Packet Lenght
                        int packetLength = packet.ReadInt();
                        //Get Packet ID
                        string packet_id = packet.ReadString();
                        //Retrieve Packet Handler
                        PacketHandlerBase packetHandler = null;
                        if (!GameServer.serverMap[0].packetHandlers.TryGetValue(packet_id, out packetHandler))
                        {
                            Debug.DebugUtility.ErrorLog(this, $"Invaild Packet ID[{packet_id}]");
                            return;
                        }
                        await packetHandler.ReadPacket(this, packet);
                    }
                }
                catch(Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"ReadMsg: {e}");
                }
            }
        }
    }
}