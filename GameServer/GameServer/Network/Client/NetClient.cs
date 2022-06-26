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
        private CancellationTokenSource sendCTS, receiveCTS;

        public NetClient(TcpClient tcpClient)
        {
            guid = Guid.NewGuid();
            this.tcpClient = tcpClient;
            sendCTS = new CancellationTokenSource();
            receiveCTS = new CancellationTokenSource();
        }

        ~NetClient()
        {
            if (tcpClient.Connected)
                tcpClient.Close();
            tcpClient.Dispose();
            sendCTS.Dispose();
            receiveCTS.Dispose();
        }

        public async void Send(Packet packet)
        {
            packet.WriteLength();
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.WriteAsync(packet.ToBytes(), 0, packet.ToBytes().Length, sendCTS.Token);
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"SendMsgAsync: {e}");
                }
            }
        }

        public async Task<Packet> Read()
        {
            Packet packet = null;
            byte[] receivedBytes = new byte[tcpClient.ReceiveBufferSize];
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.ReadAsync(receivedBytes, 0, tcpClient.ReceiveBufferSize, receiveCTS.Token);
                    packet = new Packet(receivedBytes);
                    //Get Packet Lenght
                    int packetLength = packet.ReadInt();
                    //Get Packet ID
                    string packet_id = packet.ReadString();
                    PacketHandlerBase packetHandler = null;
                    if (!Server.packetHandlers.TryGetValue(packet_id, out packetHandler))
                    {
                        Debug.DebugUtility.ErrorLog(this, $"Invaild Packet ID[{packet_id}]");
                        return packet;
                    }
                    await packetHandler.ReadPacket(this, packet);
                }
                catch(Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"ReadMsg: {e}");
                }
            }
            return packet;
        }

        public void ForceStopSendMsg()
        {
            sendCTS.Cancel();
            //Send Cancel Error Msg
        }

        public void ForceStopReadMsg()
        {
            receiveCTS.Cancel();
            tcpClient.ReceiveBufferSize = 0;
        }
    }
}