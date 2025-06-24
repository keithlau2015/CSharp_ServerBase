using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityClient
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("Connection Settings")]
        public string serverIP = "127.0.0.1";
        public int tcpPort = 439500;
        public int udpPort = 539500;
        
        [Header("Player Settings")]
        public string playerName = "UnityPlayer";
        public bool autoConnect = false;
        
        [Header("Debug")]
        public bool enableDebugLogs = true;
        
        // Network components
        private TcpClient tcpClient;
        private UdpClient udpClient;
        private NetworkStream tcpStream;
        private string playerUID;
        private bool isConnected = false;
        private bool isRunning = false;
        
        // Packet handlers
        private Dictionary<string, Action<object>> packetHandlers = new Dictionary<string, Action<object>>();
        
        // Events
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string> OnConnectionError;
        public static event Action<string, object> OnPacketReceived;
        
        // Singleton
        public static NetworkManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePacketHandlers();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (autoConnect)
            {
                ConnectToServer();
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        #region Connection Management
        
        public async void ConnectToServer()
        {
            await ConnectToServerAsync();
        }
        
        public async Task<bool> ConnectToServerAsync()
        {
            if (isConnected)
            {
                DebugLog("⚠️ Already connected to server");
                return true;
            }
            
            try
            {
                DebugLog($"🔗 Connecting to server: {serverIP}:{tcpPort}");
                
                // Generate unique player UID
                playerUID = System.Guid.NewGuid().ToString();
                
                // TCP Connection for reliable data
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIP, tcpPort);
                tcpStream = tcpClient.GetStream();
                
                // UDP Connection for real-time data  
                udpClient = new UdpClient();
                udpClient.Connect(serverIP, udpPort);
                
                isConnected = true;
                isRunning = true;
                
                DebugLog("✅ Connected to server successfully!");
                
                // Start listening for messages
                _ = Task.Run(ListenForTcpMessages);
                
                // Send initial connection packet
                await SendHeartbeat();
                
                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"❌ Connection failed: {ex.Message}");
                OnConnectionError?.Invoke(ex.Message);
                Disconnect();
                return false;
            }
        }
        
        public void Disconnect()
        {
            if (!isConnected) return;
            
            isRunning = false;
            isConnected = false;
            
            try
            {
                tcpStream?.Close();
                tcpClient?.Close();
                udpClient?.Close();
            }
            catch (Exception ex)
            {
                DebugLog($"⚠️ Error during disconnect: {ex.Message}");
            }
            
            DebugLog("🔌 Disconnected from server");
            OnDisconnected?.Invoke();
        }
        
        #endregion
        
        #region Packet Sending
        
        public async Task SendTcpPacket(string packetType, object data = null)
        {
            if (!isConnected || tcpStream == null) 
            {
                DebugLog("❌ Cannot send TCP packet: Not connected");
                return;
            }
            
            try
            {
                var packet = new UnityPacket(packetType, playerUID, data);
                byte[] packetData = packet.ToBytes();
                
                await tcpStream.WriteAsync(packetData, 0, packetData.Length);
                DebugLog($"📤 Sent TCP: {packetType}");
            }
            catch (Exception ex)
            {
                DebugLog($"❌ TCP send error: {ex.Message}");
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Disconnect();
                }
            }
        }
        
        public async Task SendUdpPacket(string packetType, object data = null)
        {
            if (!isConnected || udpClient == null)
            {
                DebugLog("❌ Cannot send UDP packet: Not connected");
                return;
            }
            
            try
            {
                var packet = new UnityPacket(packetType, playerUID, data);
                byte[] packetData = packet.ToBytes();
                
                await udpClient.SendAsync(packetData, packetData.Length);
                DebugLog($"📤 Sent UDP: {packetType}");
            }
            catch (Exception ex)
            {
                DebugLog($"❌ UDP send error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Packet Receiving
        
        private async Task ListenForTcpMessages()
        {
            byte[] lengthBuffer = new byte[4];
            
            while (isRunning && tcpClient != null && tcpClient.Connected)
            {
                try
                {
                    // Read packet length first
                    int bytesRead = 0;
                    while (bytesRead < 4)
                    {
                        int read = await tcpStream.ReadAsync(lengthBuffer, bytesRead, 4 - bytesRead);
                        if (read == 0)
                        {
                            DebugLog("🔌 Server closed connection");
                            break;
                        }
                        bytesRead += read;
                    }
                    
                    if (bytesRead < 4) break;
                    
                    // Get packet length
                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (packetLength <= 0 || packetLength > 65536) // Sanity check
                    {
                        DebugLog($"⚠️ Invalid packet length: {packetLength}");
                        continue;
                    }
                    
                    // Read packet data
                    byte[] packetBuffer = new byte[packetLength];
                    bytesRead = 0;
                    while (bytesRead < packetLength)
                    {
                        int read = await tcpStream.ReadAsync(packetBuffer, bytesRead, packetLength - bytesRead);
                        if (read == 0) break;
                        bytesRead += read;
                    }
                    
                    if (bytesRead < packetLength) break;
                    
                    // Process packet on main thread
                    UnityMainThreadDispatcher.Instance.Enqueue(() => {
                        ProcessReceivedPacket(packetBuffer);
                    });
                }
                catch (Exception ex)
                {
                    DebugLog($"❌ TCP receive error: {ex.Message}");
                    break;
                }
            }
            
            // Connection lost
            if (isConnected)
            {
                Disconnect();
            }
        }
        
        private void ProcessReceivedPacket(byte[] data)
        {
            try
            {
                var packet = UnityPacket.FromBytes(data);
                if (packet == null) return;
                
                DebugLog($"📥 Received: {packet.PacketType}");
                HandleServerPacket(packet);
            }
            catch (Exception ex)
            {
                DebugLog($"❌ Packet processing error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Packet Handling
        
        private void InitializePacketHandlers()
        {
            // Lobby packets
            packetHandlers["CreateRoomResponse"] = OnRoomCreated;
            packetHandlers["JoinRoomResponse"] = OnRoomJoined;
            packetHandlers["LeaveRoomResponse"] = OnRoomLeft;
            packetHandlers["GetRoomListResponse"] = OnRoomListReceived;
            packetHandlers["PlayerJoinedRoom"] = OnPlayerJoinedRoom;
            packetHandlers["PlayerLeftRoom"] = OnPlayerLeftRoom;
            
            // Gameplay packets
            packetHandlers["PlayerPositionBroadcast"] = OnPlayerPositionUpdate;
            packetHandlers["PlayerActionBroadcast"] = OnPlayerAction;
            packetHandlers["GameStateBroadcast"] = OnGameStateUpdate;
            
            // Communication packets
            packetHandlers["ChatMessage"] = OnChatMessage;
            packetHandlers["ServerBroadcast"] = OnServerBroadcast;
            
            // VoIP packets
            packetHandlers["AudioBroadcast"] = OnAudioReceived;
            packetHandlers["VoiceStateBroadcast"] = OnVoiceStateChanged;
            
            // Server management
            packetHandlers["PlayerKickNotification"] = OnPlayerKicked;
            packetHandlers["ServerShutdown"] = OnServerShutdown;
            packetHandlers["PongResponse"] = OnPongReceived;
        }
        
        private void HandleServerPacket(UnityPacket packet)
        {
            if (packetHandlers.TryGetValue(packet.PacketType, out var handler))
            {
                try
                {
                    handler.Invoke(packet.Data);
                }
                catch (Exception ex)
                {
                    DebugLog($"❌ Error handling packet {packet.PacketType}: {ex.Message}");
                }
            }
            else
            {
                DebugLog($"⚠️ No handler for packet type: {packet.PacketType}");
            }
            
            // Notify listeners
            OnPacketReceived?.Invoke(packet.PacketType, packet.Data);
        }
        
        #endregion
        
        #region Lobby Functions
        
        public async Task CreateRoom(string roomName, int maxPlayers = 8, bool isPrivate = false, string password = "")
        {
            var createRoomData = new
            {
                RoomName = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                Password = password
            };
            
            await SendTcpPacket("CreateRoomRequest", createRoomData);
            DebugLog($"🏠 Requested to create room: {roomName}");
        }
        
        public async Task JoinRoom(string roomId, string password = "")
        {
            var joinRoomData = new
            {
                RoomId = roomId,
                PlayerName = playerName,
                Password = password
            };
            
            await SendTcpPacket("JoinRoomRequest", joinRoomData);
            DebugLog($"🚪 Requested to join room: {roomId}");
        }
        
        public async Task LeaveRoom()
        {
            await SendTcpPacket("LeaveRoomRequest", null);
            DebugLog("🚪 Requested to leave room");
        }
        
        public async Task GetRoomList()
        {
            await SendTcpPacket("GetRoomListRequest", null);
            DebugLog("📋 Requested room list");
        }
        
        public async Task SetPlayerReady(bool ready)
        {
            var readyData = new { IsReady = ready };
            await SendTcpPacket("PlayerReadyRequest", readyData);
            DebugLog($"✋ Set ready status: {ready}");
        }
        
        #endregion
        
        #region Gameplay Functions
        
        public async Task SendPositionUpdate(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            var positionData = new
            {
                X = position.x,
                Y = position.y,
                Z = position.z,
                RotationX = rotation.x,
                RotationY = rotation.y,
                RotationZ = rotation.z,
                RotationW = rotation.w,
                VelocityX = velocity.x,
                VelocityY = velocity.y,
                VelocityZ = velocity.z
            };
            
            await SendUdpPacket("PlayerPositionUpdate", positionData);
        }
        
        public async Task SendPlayerAction(string actionType, string actionData = "")
        {
            var action = new
            {
                ActionType = actionType,
                ActionData = actionData,
                Timestamp = DateTime.UtcNow
            };
            
            await SendUdpPacket("PlayerAction", action);
            DebugLog($"🎯 Sent action: {actionType}");
        }
        
        public async Task SendChatMessage(string message, string messageType = "general")
        {
            var chatData = new
            {
                Message = message,
                MessageType = messageType
            };
            
            await SendTcpPacket("ChatMessage", chatData);
            DebugLog($"💬 Sent chat: {message}");
        }
        
        public async Task SendHeartbeat()
        {
            await SendTcpPacket("Heartbeat", null);
        }
        
        public async Task SendPing()
        {
            var pingData = new { Timestamp = DateTime.UtcNow };
            await SendUdpPacket("PingRequest", pingData);
        }
        
        #endregion
        
        #region Packet Handlers (Events)
        
        private void OnRoomCreated(object data)
        {
            DebugLog("✅ Room created successfully!");
        }
        
        private void OnRoomJoined(object data)
        {
            DebugLog("✅ Joined room successfully!");
        }
        
        private void OnRoomLeft(object data)
        {
            DebugLog("✅ Left room successfully!");
        }
        
        private void OnRoomListReceived(object data)
        {
            DebugLog("📋 Received room list");
        }
        
        private void OnPlayerJoinedRoom(object data)
        {
            DebugLog("👤 Another player joined the room");
        }
        
        private void OnPlayerLeftRoom(object data)
        {
            DebugLog("👋 Another player left the room");
        }
        
        private void OnPlayerPositionUpdate(object data)
        {
            // Handle other players' position updates
        }
        
        private void OnPlayerAction(object data)
        {
            // Handle other players' actions (jump, shoot, etc.)
        }
        
        private void OnGameStateUpdate(object data)
        {
            // Handle game state changes (game start, end, etc.)
        }
        
        private void OnChatMessage(object data)
        {
            DebugLog("💬 Received chat message");
        }
        
        private void OnServerBroadcast(object data)
        {
            DebugLog("📢 Received server broadcast");
        }
        
        private void OnAudioReceived(object data)
        {
            // Handle VoIP audio from other players
        }
        
        private void OnVoiceStateChanged(object data)
        {
            // Handle VoIP state changes (muted, talking, etc.)
        }
        
        private void OnPlayerKicked(object data)
        {
            DebugLog("👮 A player was kicked from the server");
        }
        
        private void OnServerShutdown(object data)
        {
            DebugLog("🛑 Server is shutting down");
            Disconnect();
        }
        
        private void OnPongReceived(object data)
        {
            DebugLog("🏓 Pong received");
        }
        
        #endregion
        
        #region Utility
        
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[NetworkManager] {message}");
            }
        }
        
        public bool IsConnected => isConnected;
        public string PlayerUID => playerUID;
        public string PlayerName => playerName;
        
        #endregion
    }
} 