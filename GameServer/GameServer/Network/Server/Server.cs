using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Network
{
    public class Server
    {   
        private const int HEART_BEAT_INTERVAL = 60000;
        private TcpListener tcpListener = null;
        private UdpClient udpClient = null;        
        public ServerStatus serverStatus;
        private int ticks;
        

        #region Port
        private readonly int DEFAULT_TCP_PORT = 439500;
        private readonly int DEFAULT_UDP_PORT = 539500;
        private int tcpPort = 0;
        private int udpPort = 0;
        #endregion

        #region Net Client
        private ConcurrentDictionary<string, NetClient> netClientMap = new ConcurrentDictionary<string, NetClient>();
        #endregion

        public Dictionary<string, PacketHandlerBase> packetHandlers = new Dictionary<string, PacketHandlerBase>();


        public Server(int tcpPort = 0, int udpPort = 0)
        {
            //TCP port validation
            if(tcpPort <= 1024 || tcpPort > 65535 || IsPortOccupied(tcpPort))
                tcpPort = DEFAULT_TCP_PORT;

            //UPD port validation
            if(udpPort <= 1024 || udpPort > 65535)
                udpPort = DEFAULT_UDP_PORT;

            this.tcpPort = tcpPort;
            this.udpPort = udpPort;

            #region Packet Handler
            packetHandlers.Add("Heartbeat", new GenericPacketHandler<Packet>(PreformHeartBeat));
            packetHandlers.Add(typeof(ServerStatus).ToString(), new GenericPacketHandler<Packet>(ResponseServerStatus));
            #endregion
        }

        #region Execute & Terminate
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
            //Get Current Computer Name
            string hostName = Dns.GetHostName();
            //Get Current Computer IP
            IPAddress[] ipa = Dns.GetHostAddresses(hostName);
            Debug.DebugUtility.DebugLog(this, $"Starting up TCP Lienter IP[{ipa[0]}]");
            
            //Create IP End Point
            IPEndPoint ipe = new IPEndPoint(ipa[0], this.tcpPort);
            tcpListener = new TcpListener(ipe);
            tcpListener.Start();
            Debug.DebugUtility.DebugLog(this, $"TCP Server Start Up");

            TcpClient tcpClient;
            while (true)
            {
                try
                {
                    tcpClient = tcpListener.AcceptTcpClient();

                    if (tcpClient.Connected)
                    {
                        Debug.DebugUtility.DebugLog(this, "Client Connected!");
                        NetClient netClient = new NetClient(tcpClient);
                        if(!netClientMap.TryAdd(netClient.guid.ToString(), netClient))
                        {
                            tcpClient.Close();
                            Debug.DebugUtility.ErrorLog(this, "NetClient add to map failed!");
                            continue;
                        }

                        //Create New Thread
                        Thread clientThread = new Thread(new ThreadStart(async() => { await netClient.Read(); }));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                        clientThread.Name = tcpClient.Client.RemoteEndPoint.ToString();
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
            //Init Server Status
            serverStatus = new ServerStatus(0, "Test Server", (int)ServerStatus.Status.standard, TimeManager.singleton.GetServerTime());
        }

        public void ShutDown()
        {
            
        }
        #endregion
        
        #region Send Packet
        public void SendPacket(NetClient netClient, Packet packet)
        {
            if (netClient == null || packet == null)
                return;
            netClient.Send(packet);
        }

        public void Broadcast(Packet packet)
        {
            if(packet == null)
            {
                Debug.DebugUtility.ErrorLog(this, $"Params Null[packet => {packet == null}]");
                return;
            }

            foreach (NetClient netClient in netClientMap.Values)
            {
                if (netClient == null)
                    continue;
                netClient.Send(packet);
            }
        }

        public void BroadcastSpecificGrp(List<string> netClientIDList, Packet packet)
        {
            if(netClientIDList == null || packet == null)
            {
                Debug.DebugUtility.ErrorLog(this, $"Params Null[netClientIDList => {netClientIDList == null}, packet => {packet == null}]");
                return;
            }

            foreach(string guid in netClientIDList)
            {
                NetClient netClient = null;
                if(!netClientMap.TryGetValue(guid, out netClient))
                    continue;
                netClient.Send(packet);
            }
        }
        #endregion

        #region Heartbeat
        private void PreformHeartBeat(NetClient netClient, Packet packet)
        {

            using(Packet response = new Packet("ResponseHeartbeat"))
            {
                netClient.Send(response);
            }
        }
        
        #endregion

        #region ServerStatus
        private void ResponseServerStatus(NetClient netClient, Packet packet)
        {
            using(Packet response = new Packet("ResponseServerStatus"))
            {
                response.Write(this.serverStatus);
                netClient.Send(response);
            }            
        }

        private void UpdateCurrentServerStatus(int currentClientNum)
        {
            if(serverStatus == null)
                serverStatus = new ServerStatus(0, "", (int)ServerStatus.Status.standard, TimeManager.singleton.GetServerTime());
            
            int status = (int)ServerStatus.Status.standard;

            serverStatus.UpdateStatus(status);
        }
        #endregion
    }
}