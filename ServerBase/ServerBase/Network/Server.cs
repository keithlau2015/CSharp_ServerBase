using System;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    public class Server
    {
        private TcpListener tcpListener = null;
        private UdpClient udpListener = null;

#region Port
        private Int32 tcpPort = 0;
        private Int32 udpPort = 0;
#endregion

        public Server(Int32 tcpPort = 439500, Int32 udpPort = 539500)
        {
            this.tcpPort = tcpPort;
            this.udpPort = udpPort;
        }

        public void StartUDP()
        {
            if (udpPort == 0)
            {
                Debug.DebugUtility.ErrorLog(this, "UDP Port is 0");
                return;
            }

            udpListener = new UdpClient(this.udpPort);
            IPEndPoint iPE = new IPEndPoint(IPAddress.Any, tcpPort);
            try
            {
                while (true)
                {

                }
            }
            catch (SocketException e)
            {
                Debug.DebugUtility.ErrorLog(this, $"{e}");
            }
            finally
            {
                udpListener.Close();
            }
        }

        public void StartTCP()
        {
            if (tcpPort == 0)
            {
                Debug.DebugUtility.ErrorLog(this, "TCP Port is 0");
                return;
            }

            tcpListener.Start();
            while (true)
            {
                try
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                }
                catch (Exception e)
                {
                    Debug.DebugUtility.ErrorLog(this, $"{e}");
                }
            }
        }

        public void Run()
        {
                        
        }

        public void ShutDown()
        {
            
        }
    }
}