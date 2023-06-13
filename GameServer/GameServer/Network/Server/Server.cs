using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net.Http;

namespace Network
{
    public class Server
    {        
        #region CONST
        private const int MAX_NETCLIENT = 1000;
        private const int CHECK_TIMEOUT_INTERVAL = 300;
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
        public static ConcurrentDictionary<string, NetClient> timeoutPendingNetClientMap = new ConcurrentDictionary<string, NetClient>();

        //Packet Handler
        public static Dictionary<string, PacketHandlerBase> packetHandlers = new Dictionary<string, PacketHandlerBase>();

        //Server Status
        public static ServerStatus serverStatus { get; private set; } = null;

        public static async Task Main(string[] args)
        {
            #region Server Config
            IConfigurationRoot config = new ConfigurationBuilder().SetBasePath($"{Directory.GetCurrentDirectory()}/Config").AddJsonFile("appconfig.json").Build();
            IConfigurationSection section = config.GetSection(nameof(ServerConfig));
            ServerConfig serverConfig = section.Get<ServerConfig>();
            #endregion

            #region Init DeugUtility
            Debug.DebugUtility.Init(serverConfig.DebugLevel);
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
            packetHandlers.Add("Heartbeat", new HeartbeatHandler());
            packetHandlers.Add(typeof(ServerStatus).ToString(), new GenericPacketHandler<Packet>(ResponseServerStatus));

            #endregion

            //Server Status
            serverStatus = new ServerStatus(serverConfig.ID, serverConfig.Name, (int)ServerStatus.Status.standard);

            //Get Current Computer Name
            string hostName = Dns.GetHostName();
            //Get Current Computer IP
            IPAddress[] ipa = Dns.GetHostAddresses(hostName, AddressFamily.InterNetwork);
            int publicIpaIndex = 0;
            for (int i = 0; i < ipa.Length; i++)
            {
                IPAddress iPAddress = ipa[i];

                //validation for public ip
                string[] ipAddressSplit = iPAddress.ToString().Split('.');
                if (ipAddressSplit.Length == 0)
                    continue;

                if (ipAddressSplit[0].Trim().Equals("192"))
                    continue;

                publicIpaIndex = i;
                break;
            }
            //Debug.DebugUtility.DebugLog($"Starting up TCP Lienter IP[{ipa[publicIpaIndex]}]");

            //Init TCP
            IPEndPoint tcpIPE = new IPEndPoint(ipa[0], tcpPort);
            tcpListener = new TcpListener(tcpIPE);
            tcpListener.Start();
            Debug.DebugUtility.DebugLog($"TCP Server Start Up [{serverStatus.Name}] With Port [{tcpPort}]");
            //Init UDP
            IPEndPoint udpIPE = new IPEndPoint(ipa[0], udpPort);
            udpListener = new UdpClient(udpIPE);
            Debug.DebugUtility.DebugLog($"UDP Server Start Up [{serverStatus.Name}] With Port [{udpPort}]");

            while (true)
            {
                #region Start TCP Protocol
                using (tcpCTS.Token.Register(() => tcpListener.Stop()))
                {
                    if (!tcpCTS.Token.IsCancellationRequested)
                    {
                        await Task.Yield();
                        try
                        {
                            TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                            if (tcpClient.Connected)
                            {
                                Debug.DebugUtility.DebugLog("TCP Client Connected!");

                                NetClient netClient = new NetClient();
                                netClient.tcProtocol.SetUp(tcpClient);
                                if(!netClientMap.TryAdd(netClient.UID.ToString(), netClient))
                                {
                                    Debug.DebugUtility.WarningLog("TCP Client failed to cache!");
                                    continue;
                                }

                                //Update server status
                                UpdateCurrentServerStatus();

                                //Create New Thread
                                Thread clientThread = new Thread(new ThreadStart(() =>
                                {
                                    while (netClient.isAlive)
                                    {
                                        netClient.Read();
                                    }
                                    //If netClient is not alive
                                    netClient.Disconnect();
                                }));
                                clientThread.IsBackground = true;
                                clientThread.Start();
                                clientThread.Name = $"{netClient.UID.ToString()}_TCP";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.DebugUtility.ErrorLog(ex.Message);
                        }
                    }
                }
                #endregion

                #region Start UDP Protocol
                using (udpCTS.Token.Register(() => udpListener.Close()))
                {
                    if (!udpCTS.Token.IsCancellationRequested)
                    {
                        await Task.Yield();                        
                        try
                        {
                            if (!udpCTS.Token.IsCancellationRequested)
                            {
                                UdpReceiveResult udpReceiveResult = await udpListener.ReceiveAsync(udpCTS.Token);
                                if (udpReceiveResult.Buffer.Length < 4)
                                    continue;

                                string clientUID = "";
                                using (Packet packet = new Packet(udpReceiveResult.Buffer))
                                {
                                    //Get Packet Lenght
                                    int packetLength = packet.ReadInt();
                                    //Get Packet ID
                                    string packet_id = packet.ReadString();
                                    
                                    clientUID = packet.ReadString();
                                }

                                NetClient netClient = null;
                                if (!netClientMap.TryGetValue(clientUID, out netClient))
                                    continue;

                                //Update server status
                                UpdateCurrentServerStatus();

                                //Create New Thread
                                Thread clientThread = new Thread(new ThreadStart(() =>
                                {
                                    while (netClient.isAlive)
                                    {
                                        netClient.Read(supportProtocol: NetClient.SupportProtocol.UDP);
                                    }
                                    //If netClient is not alive
                                    netClient.Disconnect();
                                }));
                                clientThread.IsBackground = true;
                                clientThread.Start();
                                clientThread.Name = $"{netClient.UID.ToString()}_UDP";
                            }
                        }
                        catch (SocketException e)
                        {
                            Debug.DebugUtility.ErrorLog($"{e}");
                        }
                        finally
                        {
                            udpListener.Close();
                        }
                    }
                }
                #endregion
            }
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

        #region ServerStatus
        private static void ResponseServerStatus(NetClient netClient, Packet packet)
        {
            using (Packet response = new Packet("ResponseServerStatus"))
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

        #region Time out netClient
        private void KickoutTimeoutNetClient()
        {
            foreach(NetClient netClient in netClientMap.Values)
            {
                if(netClient == null)
                {
                    netClientMap.TryRemove(netClient.UID.ToString(), out _);
                    continue;
                }

                if (!netClient.isAlive)
                    netClient.Disconnect();
            }
        }
        #endregion
    }
}