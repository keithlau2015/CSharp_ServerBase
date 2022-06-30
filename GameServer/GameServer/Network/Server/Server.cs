using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Common;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Network
{
    public class Server
    {        
        #region CONST
        private const int MAX_NETCLIENT = 1000;
        #endregion
        
        private static TcpListener tcpListener = null;
        private static UdpClient udpListener = null;        
        private static CancellationTokenSource tcpCTS, udpCTS;

        #region Port
        private static readonly int DEFAULT_TCP_PORT = 439500;
        private static readonly int DEFAULT_UDP_PORT = 539500;
        private static int tcpPort = 0;
        private static int udpPort = 0;
        #endregion

        //Net Client
        public static ConcurrentDictionary<string, NetClient> netClientMap = new ConcurrentDictionary<string, NetClient>();
        //Packet Handler
        public static Dictionary<string, PacketHandlerBase> packetHandlers = new Dictionary<string, PacketHandlerBase>();
        //Server Status
        public static ServerStatus serverStatus { get; private set; } = null;

        public static void Main(string[] args)
        {
            #region Server Config
            IConfigurationRoot config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddJsonFile("ServerConfig.json").Build();
            IConfigurationSection section = config.GetSection(nameof(ServerConfig));
            List<ServerConfig> serverConfig = section.Get<List<ServerConfig>>();
            #endregion

            #region DB connection
            #endregion

            #region TCP & UDP Init
            //TCP port validation
            if (tcpPort <= 1024 || tcpPort > 65535 || IsPortOccupied(tcpPort))
                tcpPort = DEFAULT_TCP_PORT;

            //UPD port validation
            if (udpPort <= 1024 || udpPort > 65535)
                udpPort = DEFAULT_UDP_PORT;

            tcpPort = serverConfig.TCPPort;
            udpPort = serverConfig.UDPPort;

            tcpCTS = new CancellationTokenSource();
            udpCTS = new CancellationTokenSource();
            #endregion
            
            #region Packet Handler
            packetHandlers.Add("Heartbeat", new GenericPacketHandler<Packet>(PreformHeartBeat));
            packetHandlers.Add(typeof(ServerStatus).ToString(), new GenericPacketHandler<Packet>(ResponseServerStatus));
            #endregion

            //Start TCP Listener
            ThreadPool.QueueUserWorkItem(new WaitCallback(StartTCP), tcpCTS.Token);
            //Start UDP Listener
            ThreadPool.QueueUserWorkItem(new WaitCallback(StartUDP), udpCTS.Token);
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
            using(udpCTS.Token.Register(() => udpListener.Close()))
            {
                udpListener = new UdpClient(udpPort);
                IPEndPoint iPE = new IPEndPoint(IPAddress.Any, udpPort);
                try
                {
                    while (!udpCTS.Token.IsCancellationRequested)
                    {
                        using(Packet packet = new Packet(await udpListener.ReceiveAsync(udpCTS.Token)))
                        {
                            
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.DebugUtility.ErrorLog($"{e}");
                }
                finally
                {
                    udpClient.Close();
                }
            }           
        }

        public static async void StartTCP()
        {
            //Get Current Computer Name
            string hostName = Dns.GetHostName();
            //Get Current Computer IP
            IPAddress[] ipa = Dns.GetHostAddresses(hostName);
            Debug.DebugUtility.DebugLog($"Starting up TCP Lienter IP[{ipa[0]}]");
            
            //Create IP End Point
            IPEndPoint ipe = new IPEndPoint(ipa[0], tcpPort);
            tcpListener = new TcpListener(ipe);
            tcpListener.Start();
            Debug.DebugUtility.DebugLog($"TCP Server Start Up");

            using(tcpCTS.Token.Register(() => tcpListener.Stop()))
            {
                while (!tcpCTS.Token.IsCancellationRequested)
                {
                    await Task.Yield();
                    try
                    {
                        using (TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync(tcpCTS.Token))
                        {
                            if (tcpClient.Connected)
                            {
                                Debug.DebugUtility.DebugLog("Client Connected!");
                                NetClient netClient = new NetClient(tcpClient);
                                if (!netClientMap.TryAdd(netClient.UID.ToString(), netClient))
                                {
                                    tcpClient.Close();
                                    Debug.DebugUtility.ErrorLog("NetClient add to map failed!");
                                    continue;
                                }

                                //Update server status
                                UpdateCurrentServerStatus();

                                //Create New Thread
                                Thread clientThread = new Thread(new ThreadStart(async () => { await netClient.Read(); }));
                                clientThread.IsBackground = true;
                                clientThread.Start();
                                clientThread.Name = tcpClient.Client.RemoteEndPoint.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.DebugUtility.ErrorLog(ex.Message);
                    }
                }
            }
        }

        public static void ShutDown(bool tcp = true, bool udp = true)
        {
            if(tcp)
            {
                if(!tcpCTS.IsCancellationRequested)
                    tcpCTS.Cancel();
            }
            if(udp)
            {
                if(!udpCTS.IsCancellationRequested)
                    udpCTS.Cancel();
            }
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
                Debug.DebugUtility.ErrorLog($"Params Null[packet => {packet == null}]");
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
                Debug.DebugUtility.ErrorLog($"Params Null[netClientIDList => {netClientIDList == null}, packet => {packet == null}]");
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
        private static void PreformHeartBeat(NetClient netClient, Packet packet)
        {
            netClient.PreformHeartBeat(packet);
            using(Packet response = new Packet("ResponseHeartbeat"))
            {
                //Current Server Time
                response.Write(TimeManager.singleton.GetServerTime());
                netClient.Send(response);
            }
        }        
        #endregion

        #region ServerStatus
        private static void ResponseServerStatus(NetClient netClient, Packet packet)
        {
            using(Packet response = new Packet("ResponseServerStatus"))
            {
                response.Write(serverStatus);
                netClient.Send(response);
            }
        }

        private static void UpdateCurrentServerStatus()
        {
            if(serverStatus == null)
            {
                Debug.DebugUtility.ErrorLog($"Try to update server status, but server status is null");
                return;
            }
            
            int status = serverStatus.CurStatus;
            if (status <= (int)ServerStatus.Status.crowd && netClientMap.Count >= (int)(MAX_NETCLIENT * 0.9))
            {
                status = (int)ServerStatus.Status.crowd;
                serverStatus.UpdateStatus((ServerStatus.Status)status);
            }
        }
        #endregion
    }
}