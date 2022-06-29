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
        private const int HEART_BEAT_TIMEOUT = 60000;

        public Guid UID { get; private set; }
        private TcpClient tcpClient;
        public long LastHeartbeatUnixtimestamp { get; private set;}
        public bool IsAlive { get; private set; }
        public NetClient(TcpClient tcpClient)
        {
            UID = Guid.NewGuid();
            this.tcpClient = tcpClient;
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
                    Debug.DebugUtility.ErrorLog($"SendMsgAsync: {e}");
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
                        if (!Server.packetHandlers.TryGetValue(packet_id, out packetHandler))
                        {
                            Debug.DebugUtility.ErrorLog($"Invaild Packet ID[{packet_id}]");
                            return;
                        }
                        await packetHandler.ReadPacket(this, packet);
                    }
                }
                catch(Exception e)
                {
                    Debug.DebugUtility.ErrorLog($"ReadMsg: {e}");
                }
            }
        }

        public void PreformHeartBeat(Packet packet)
        {            
            LastHeartbeatUnixtimestamp = packet.ReadLong();
            IsAlive = true;
            System.Timers.Timer timer = new System.Timers.Timer(HEART_BEAT_TIMEOUT);
            timer.Elapsed += (Object source, ElapsedEventArgs e) => {
                Disconnect();
                Debug.DebugUtility.DebugLog($"Unable receive within Heartbeat Interval range, kick out client[{UID.ToString()}]");
            };
            timer.Enabled = true;
        }

        public void Disconnect()
        {
            if(Server.netClientMap == null)
                return;

            Server.netClientMap.TryRemove(UID.ToString(), out _);
            if(tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
            LastHeartbeatUnixtimestamp = 0;
            IsAlive = false;
            Debug.DebugUtility.DebugLog($"NetClient[{UID.ToString()}] Disconnected");
        }
    }
}