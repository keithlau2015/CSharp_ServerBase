using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Network
{
    public class Server
    {
        private readonly int DEFAULT_TCP_PORT = 439500;
        private readonly int DEFAULT_UDP_PORT = 539500;
        private TcpListener tcpListener = null;
        private UdpClient udpClient = null;

        private CancellationTokenSource tcpCTS, udpCTS;

#region Port
        private int tcpPort = 0;
        private int udpPort = 0;
#endregion

        public Server(int tcpPort = DEFAULT_TCP_PORT, int udpPort = DEFAULT_UDP_PORT)
        {
            //TCP port validation
            if(tcpPort <= 1024 || tcpListener > 65535 || tcpPort == udpPort || IsPortOccupied(tcpPort))
                tcpPort = DEFAULT_TCP_PORT;

            //UPD port validation
            if(udpPort <= 1024 || udpPort > 65535)
                udpPort = DEFAULT_UDP_PORT;

            this.tcpPort = tcpPort;
            this.udpPort = udpPort;
        }

        public static bool IsPortOccupied(int port)
        {
            bool result = false;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port==port)
                {
                    result = true;
                    break;
                }
            }            
            return result;
        }

        public void StartUDP()
        {
            udpCTS = new CancellationTokenSource();
            CancellationToken token = udpCTS.token;
            udpClient = new UdpClient(this.udpPort);
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
                udpClient.Close();
            }
        }

        public void StartTCP()
        {
            tcpCTS = new CancellationTokenSource();
            CancellationToken token = tcpCTS.token;

            //Get Current Computer Name
            string hostName = Dns.GetHostName();
            //Get Current Computer IP
            IPAddress[] ipa = Dns.GetHostAddresses(hostName);
            Debug.DebugUtility.Log(this, $"Starting up TCP Lienter IP[{ipa[0]}]");
            
            //Create IP End Point
            IPEndPoint ipe = new IPEndPoint(ipa[0], this.tcpPort);
            tcpListener = new TcpListener(ipe);
            tcpListener.Start();
            Debug.DebugUtility.Log(this, $"TCP Server Start Up");

            TcpClient tcpClient;
            int numOfClients = 0;
            while (true)
            {
                try
                {
                    tcpClient = tcpListener.AcceptTcpClient();

                    if (tcpClient.Connected)
                    {
                        Debug.DebugUtility.Log(this, "Client Connected!");
                        NetClient netClient = new NetClient(tcpClient);
                        Thread myThread = new Thread(new ThreadStart(netClient.Communicate));
                        numOfClients += 1;
                        myThread.IsBackground = true;
                        myThread.Start();
                        myThread.Name = tcpClient.Client.RemoteEndPoint.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog(this, ex.Message);
                    Console.Read();
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