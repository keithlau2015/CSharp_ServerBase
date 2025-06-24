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
            // First set ports from config
            tcpPort = serverConfig.TCPPort;
            udpPort = serverConfig.UDPPort;

            //TCP port validation
            if (tcpPort <= 1024 || tcpPort > 65535 || IsPortOccupied(tcpPort))
            {
                Debug.DebugUtility.WarningLog($"TCP Port {tcpPort} is invalid or occupied, using default {DEFAULT_TCP_PORT}");
                tcpPort = DEFAULT_TCP_PORT;
            }

            //UDP port validation
            if (udpPort <= 1024 || udpPort > 65535 || IsPortOccupied(udpPort))
            {
                Debug.DebugUtility.WarningLog($"UDP Port {udpPort} is invalid or occupied, using default {DEFAULT_UDP_PORT}");
                udpPort = DEFAULT_UDP_PORT;
            }

            tcpCTS = new CancellationTokenSource();
            udpCTS = new CancellationTokenSource();
            #endregion
            
            #region Packet Handler
            // Basic handlers
            packetHandlers.Add("Heartbeat", new HeartbeatHandler());
            packetHandlers.Add(typeof(ServerStatus).ToString(), new GenericPacketHandler<Packet>(ResponseServerStatus));
            
            // Lobby handlers (TCP)
            packetHandlers.Add("CreateRoomRequest", new CreateRoomHandler());
            packetHandlers.Add("JoinRoomRequest", new JoinRoomHandler());
            packetHandlers.Add("LeaveRoomRequest", new LeaveRoomHandler());
            packetHandlers.Add("GetRoomListRequest", new GetRoomListHandler());
            packetHandlers.Add("PlayerReadyRequest", new PlayerReadyHandler());
            packetHandlers.Add("StartGameRequest", new StartGameHandler());
            packetHandlers.Add("ChatMessage", new ChatMessageHandler());
            
            // Real-time gameplay handlers (UDP preferred)
            packetHandlers.Add("PlayerPositionUpdate", new PlayerPositionUpdateHandler());
            packetHandlers.Add("PlayerAction", new PlayerActionHandler());
            packetHandlers.Add("GameStateUpdate", new GameStateUpdateHandler());
            packetHandlers.Add("PingRequest", new PingHandler());
            
            // VoIP handlers (UDP preferred for audio)
            packetHandlers.Add("AudioPacket", new AudioPacketHandler());
            packetHandlers.Add("VoiceStateUpdate", new VoiceStateUpdateHandler());
            packetHandlers.Add("VoiceSettingsUpdate", new VoiceSettingsUpdateHandler());
            packetHandlers.Add("PushToTalkState", new PushToTalkHandler());
            packetHandlers.Add("VoiceQualityMetrics", new VoiceQualityMetricsHandler());
            packetHandlers.Add("AudioDeviceRequest", new AudioDeviceRequestHandler());

            #endregion

            //Server Status
            serverStatus = new ServerStatus(serverConfig.ID, serverConfig.Name, (int)ServerStatus.Status.standard);

            //Get Current Computer Name
            string hostName = Dns.GetHostName();
            //Get Current Computer IP
            IPAddress[] ipa = Dns.GetHostAddresses(hostName, AddressFamily.InterNetwork);
            
            // Find the first non-private IP address
            IPAddress serverIP = IPAddress.Any;
            for (int i = 0; i < ipa.Length; i++)
            {
                IPAddress iPAddress = ipa[i];
                string[] ipAddressSplit = iPAddress.ToString().Split('.');
                
                if (ipAddressSplit.Length == 4)
                {
                    // Skip private IP ranges (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                    if (!ipAddressSplit[0].Trim().Equals("192") && 
                        !ipAddressSplit[0].Trim().Equals("10") &&
                        !(ipAddressSplit[0].Trim().Equals("172") && 
                          int.Parse(ipAddressSplit[1]) >= 16 && 
                          int.Parse(ipAddressSplit[1]) <= 31))
                    {
                        serverIP = iPAddress;
                        break;
                    }
                }
            }
            
            // If no public IP found, use the first available IP
            if (serverIP.Equals(IPAddress.Any) && ipa.Length > 0)
            {
                serverIP = ipa[0];
            }

            try
            {
                //Init TCP
                IPEndPoint tcpIPE = new IPEndPoint(serverIP, tcpPort);
                tcpListener = new TcpListener(tcpIPE);
                tcpListener.Start();
                Debug.DebugUtility.DebugLog($"TCP Server Start Up [{serverStatus.Name}] With Port [{tcpPort}]");
                
                //Init UDP
                IPEndPoint udpIPE = new IPEndPoint(serverIP, udpPort);
                udpListener = new UdpClient(udpIPE);
                Debug.DebugUtility.DebugLog($"UDP Server Start Up [{serverStatus.Name}] With Port [{udpPort}]");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to initialize listeners: {ex.Message}");
                return;
            }

            // Start server tasks instead of infinite loop
            var tcpTask = AcceptTcpClientsAsync(tcpCTS.Token);
            var udpTask = AcceptUdpClientsAsync(udpCTS.Token);

            // Start the admin console system
            Task.Run(() =>
            {
                try
                {
                    Admin.ConsoleCommandManager.Instance.StartConsole();
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"Console manager error: {ex.Message}");
                }
            });
            
            try
            {
                await Task.WhenAll(tcpTask, udpTask);
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Server error: {ex.Message}");
            }
        }

        private static async Task AcceptTcpClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    if (tcpClient.Connected)
                    {
                        // Check client limit
                        if (netClientMap.Count >= MAX_NETCLIENT)
                        {
                            Debug.DebugUtility.WarningLog("Maximum client limit reached, rejecting connection");
                            tcpClient.Close();
                            continue;
                        }

                        Debug.DebugUtility.DebugLog("TCP Client Connected!");

                        NetClient netClient = new NetClient();
                        netClient.tcProtocol.SetUp(tcpClient);
                        
                        // Check if client is banned (we'll check by IP and later by player ID)
                        string clientIP = ((System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                        
                        if (!netClientMap.TryAdd(netClient.UID.ToString(), netClient))
                        {
                            Debug.DebugUtility.WarningLog("TCP Client failed to cache!");
                            tcpClient.Close();
                            continue;
                        }

                        //Update server status
                        UpdateCurrentServerStatus();

                        // Use Task instead of Thread for better resource management
                        _ = Task.Run(async () => await HandleClientAsync(netClient, cancellationToken), cancellationToken);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when shutting down
                    break;
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"Error accepting TCP client: {ex.Message}");
                }
            }
        }

        private static async Task AcceptUdpClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult udpReceiveResult = await udpListener.ReceiveAsync();
                    if (udpReceiveResult.Buffer.Length < 4)
                        continue;

                    string clientUID = "";
                    try
                    {
                        using (Packet packet = new Packet(udpReceiveResult.Buffer))
                        {
                            //Get Packet Length
                            int packetLength = packet.ReadInt();
                            //Get Packet ID
                            string packet_id = packet.ReadString();
                            
                            clientUID = packet.ReadString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.DebugUtility.ErrorLog($"Error parsing UDP packet: {ex.Message}");
                        continue;
                    }

                    if (netClientMap.TryGetValue(clientUID, out NetClient netClient))
                    {
                        // Update server status
                        UpdateCurrentServerStatus();
                        
                        // Handle UDP data for existing client
                        _ = Task.Run(async () => await HandleUdpDataAsync(netClient, udpReceiveResult), cancellationToken);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when shutting down
                    break;
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"Error receiving UDP data: {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(NetClient netClient, CancellationToken cancellationToken)
        {
            try
            {
                while (netClient.isAlive && !cancellationToken.IsCancellationRequested)
                {
                    await netClient.tcProtocol.Read();
                    // Small delay to prevent CPU spinning
                    await Task.Delay(1, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Error handling TCP client {netClient.UID}: {ex.Message}");
            }
            finally
            {
                // Clean up client
                netClientMap.TryRemove(netClient.UID.ToString(), out _);
                netClient.Disconnect();
                Debug.DebugUtility.DebugLog($"TCP Client {netClient.UID} disconnected");
            }
        }

        private static async Task HandleUdpDataAsync(NetClient netClient, UdpReceiveResult udpReceiveResult)
        {
            try
            {
                // Set UDP endpoint for the client
                netClient.udProtocol.SetIPEndPoint(udpReceiveResult.RemoteEndPoint);
                
                // Process UDP data
                await Task.Run(() => netClient.Read(supportProtocol: NetClient.SupportProtocol.UDP));
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Error handling UDP data for client {netClient.UID}: {ex.Message}");
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