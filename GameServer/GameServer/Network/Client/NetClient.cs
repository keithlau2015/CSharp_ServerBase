using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public class NetClient
    {    
        private TcpClient tcpClient;
        private CancellationTokenSource cts;
        public NetClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            cts = new CancellationTokenSource();
        }

        public async void SendMsg(string header, Packet packet)
        {
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.WriteAsync(packet.ToBytes(), 0, packet.ToBytes().Length, cts.Token);
                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"SendMsgAsync: {e}");
                }
            }
        }

        public async Task<Packet> ReadMsg()
        {
            Packet packet = null;
            byte[] receivedBytes = new byte[tcpClient.ReceiveBufferSize];
            using (NetworkStream stream = tcpClient.GetStream())
            {
                try
                {
                    await stream.ReadAsync(receivedBytes, 0, tcpClient.ReceiveBufferSize);
                    packet = new Packet(receivedBytes);
                }
                catch(Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"ReadMsg: {e}");
                }
            }
            return packet;
        }
    }
}