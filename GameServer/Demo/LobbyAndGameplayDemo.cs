using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GameSystem.Lobby;
using Network;

namespace Demo
{
    /// <summary>
    /// Demo class showing how to use the Lobby and Real-time Gameplay features
    /// </summary>
    public class LobbyAndGameplayDemo
    {
        public static async Task RunDemo()
        {
            Console.WriteLine("🎮 Starting Lobby and Gameplay Demo");
            Console.WriteLine("=====================================");
            
            // Initialize lobby manager
            var lobbyManager = LobbyManager.Instance;
            
            // Subscribe to events for demo purposes
            lobbyManager.OnRoomCreated += (room) => Console.WriteLine($"✅ Room Created: {room.Name} (ID: {room.Id})");
            lobbyManager.OnRoomDestroyed += (roomId) => Console.WriteLine($"❌ Room Destroyed: {roomId}");
            lobbyManager.OnPlayerJoinedRoom += (player, room) => Console.WriteLine($"👤 {player.Name} joined {room.Name}");
            lobbyManager.OnPlayerLeftRoom += (player, room) => Console.WriteLine($"👋 {player.Name} left {room.Name}");
            
            await DemoLobbyOperations();
            await DemoRealTimeGameplay();
            
            Console.WriteLine("✅ Lobby and Gameplay Demo completed!");
            Console.WriteLine("📊 Server is ready for real connections!");
            
            // Start console command demo
            await Task.Delay(1000);
            await ConsoleCommandDemo.RunConsoleDemo();
            
            // Setup admin monitoring
            ConsoleCommandDemo.SetupAdminNotifications();
            
            Console.WriteLine("🎯 All systems operational! Server ready for production use.");
        }
        
        #region Lobby Demo
        
        private static async Task DemoLobbyOperations()
        {
            Console.WriteLine("\n📋 Lobby Operations Demo");
            Console.WriteLine("------------------------");
            
            var lobbyManager = LobbyManager.Instance;
            
            // Create some demo rooms
            var room1 = lobbyManager.CreateRoom("Deathmatch Arena", 8, false);
            var room2 = lobbyManager.CreateRoom("Team Battle", 4, true, "secret123");
            var room3 = lobbyManager.CreateRoom("Racing Circuit", 6, false);
            
            Console.WriteLine($"Created {lobbyManager.GetAllRooms().Count} rooms");
            
            // Simulate some demo players (in real scenario these would be actual NetClients)
            var demoPlayers = CreateDemoPlayers();
            
            // Demo player joining rooms
            lobbyManager.JoinRoom(demoPlayers[0].UID, room1.Id);
            lobbyManager.JoinRoom(demoPlayers[1].UID, room1.Id);
            lobbyManager.JoinRoom(demoPlayers[2].UID, room2.Id, "secret123");
            
            // Set players ready
            demoPlayers[0].SetReady(true);
            demoPlayers[1].SetReady(true);
            
            // Start game in room1
            room1.StartGame();
            
            // Show room status
            ShowRoomStatus(room1);
            ShowRoomStatus(room2);
            
            await Task.Delay(1000); // Simulate some time passing
        }
        
        #endregion
        
        #region Real-time Gameplay Demo
        
        private static async Task DemoRealTimeGameplay()
        {
            Console.WriteLine("\n🚀 Real-time Gameplay Demo");
            Console.WriteLine("---------------------------");
            
            var lobbyManager = LobbyManager.Instance;
            var room = lobbyManager.GetAllRooms().Find(r => r.IsGameStarted);
            
            if (room == null)
            {
                Console.WriteLine("No active game room found for demo");
                return;
            }
            
            var players = room.GetPlayers();
            if (players.Count < 2)
            {
                Console.WriteLine("Need at least 2 players for gameplay demo");
                return;
            }
            
            Console.WriteLine($"Simulating gameplay in room: {room.Name}");
            
            // Simulate real-time position updates
            for (int i = 0; i < 10; i++)
            {
                await SimulatePlayerMovement(players, room, i);
                await Task.Delay(100); // 10 updates per second
            }
            
            // Simulate some game actions
            await SimulateGameActions(players, room);
            
            // Show final stats
            ShowPlayerStats(players);
        }
        
        private static async Task SimulatePlayerMovement(List<Player> players, GameRoom room, int frame)
        {
            foreach (var player in players)
            {
                // Simulate movement (circular motion for demo)
                float angle = frame * 0.1f + players.IndexOf(player) * 2.0f;
                float x = (float)Math.Cos(angle) * 10f;
                float z = (float)Math.Sin(angle) * 10f;
                float y = 0f;
                
                // Update position
                player.UpdatePosition(x, y, z);
                
                // In real scenario, this would broadcast via UDP
                Console.WriteLine($"📍 {player.Name}: ({x:F2}, {y:F2}, {z:F2})");
            }
        }
        
        private static async Task SimulateGameActions(List<Player> players, GameRoom room)
        {
            Console.WriteLine("\n⚔️ Simulating Game Actions");
            
            var random = new Random();
            
            // Simulate some actions
            for (int i = 0; i < 5; i++)
            {
                var player = players[random.Next(players.Count)];
                var actions = new[] { "jump", "shoot", "attack", "interact" };
                var action = actions[random.Next(actions.Length)];
                
                Console.WriteLine($"🎯 {player.Name} performed: {action}");
                
                // Simulate action processing
                if (action == "shoot" && random.NextDouble() > 0.5)
                {
                    player.IncrementKills();
                    player.AddScore(100);
                    Console.WriteLine($"💀 {player.Name} got a kill! Score: {player.Stats.Score}");
                }
                
                await Task.Delay(500);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private static List<Player> CreateDemoPlayers()
        {
            var players = new List<Player>();
            var names = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve" };
            
            foreach (var name in names)
            {
                // In real scenario, these would be actual NetClients
                var demoNetClient = new NetClient(); // Create demo NetClient
                var player = LobbyManager.Instance.CreatePlayer(demoNetClient, name);
                if (player != null)
                {
                    players.Add(player);
                }
            }
            
            return players;
        }
        
        private static void ShowRoomStatus(GameRoom room)
        {
            Console.WriteLine($"\n🏠 Room: {room.Name}");
            Console.WriteLine($"   Status: {room.State}");
            Console.WriteLine($"   Players: {room.PlayerCount}/{room.MaxPlayers}");
            Console.WriteLine($"   Game Started: {room.IsGameStarted}");
            
            foreach (var player in room.GetPlayers())
            {
                Console.WriteLine($"   👤 {player.Name} (Ready: {player.IsReady})");
            }
        }
        
        private static void ShowPlayerStats(List<Player> players)
        {
            Console.WriteLine("\n📊 Player Statistics");
            Console.WriteLine("--------------------");
            
            foreach (var player in players)
            {
                Console.WriteLine($"👤 {player.Name}:");
                Console.WriteLine($"   Score: {player.Stats.Score}");
                Console.WriteLine($"   Kills: {player.Stats.Kills}");
                Console.WriteLine($"   Deaths: {player.Stats.Deaths}");
                Console.WriteLine($"   K/D Ratio: {player.Stats.KDRatio:F2}");
                Console.WriteLine($"   Position: ({player.Position.X:F2}, {player.Position.Y:F2}, {player.Position.Z:F2})");
                Console.WriteLine($"   Session Time: {player.GetSessionTime().TotalMinutes:F1} minutes");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Example of how a client would interact with the server
    /// </summary>
    public class ClientExample
    {
        private NetClient netClient;
        private bool isConnected = false;
        private string playerId;
        private string currentRoomId;
        
        public async Task ConnectToServer(string serverIP, int port)
        {
            try
            {
                // In real implementation, this would establish actual network connection
                netClient = new NetClient();
                isConnected = true;
                playerId = netClient.UID.ToString();
                
                Console.WriteLine($"🔗 Connected to server as {playerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to connect: {ex.Message}");
            }
        }
        
        public async Task CreateRoom(string roomName, int maxPlayers, bool isPrivate = false, string password = "")
        {
            if (!isConnected) return;
            
            var request = new CreateRoomRequest
            {
                RoomName = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                Password = password
            };
            
            var packet = new Packet("CreateRoomRequest");
            packet.Write(request);
            
            // Send packet
            netClient.Send(packet);
            Console.WriteLine($"📤 Sent create room request: {roomName}");
        }
        
        public async Task JoinRoom(string roomId, string playerName, string password = "")
        {
            if (!isConnected) return;
            
            var request = new JoinRoomRequest
            {
                RoomId = roomId,
                PlayerName = playerName,
                Password = password
            };
            
            var packet = new Packet("JoinRoomRequest");
            packet.Write(request);
            
            netClient.Send(packet);
            currentRoomId = roomId;
            Console.WriteLine($"📤 Sent join room request: {roomId}");
        }
        
        public async Task SendPositionUpdate(float x, float y, float z, float rotX = 0, float rotY = 0, float rotZ = 0, float rotW = 1)
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;
            
            var positionUpdate = new PlayerPositionUpdate
            {
                X = x,
                Y = y,
                Z = z,
                RotationX = rotX,
                RotationY = rotY,
                RotationZ = rotZ,
                RotationW = rotW,
                VelocityX = 0,
                VelocityY = 0,
                VelocityZ = 0
            };
            
            var packet = new Packet("PlayerPositionUpdate");
            packet.Write(positionUpdate);
            
            // Send via UDP for real-time data
            netClient.Send(packet, NetClient.SupportProtocol.UDP);
        }
        
        public async Task SendPlayerAction(string actionType, string actionData = "")
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;
            
            var action = new GameSystem.Lobby.PlayerAction
            {
                PlayerId = playerId,
                ActionType = actionType,
                ActionData = actionData,
                Timestamp = DateTime.UtcNow
            };
            
            var packet = new Packet("PlayerAction");
            packet.Write(action);
            
            // Send via UDP for real-time actions
            netClient.Send(packet, NetClient.SupportProtocol.UDP);
            Console.WriteLine($"🎯 Sent action: {actionType}");
        }
        
        public async Task SendChatMessage(string message)
        {
            if (!isConnected || string.IsNullOrEmpty(currentRoomId)) return;
            
            var chatMessage = new ChatMessage
            {
                Message = message,
                MessageType = "general"
            };
            
            var packet = new Packet("ChatMessage");
            packet.Write(chatMessage);
            
            netClient.Send(packet);
            Console.WriteLine($"💬 Sent chat: {message}");
        }
        
        public void Disconnect()
        {
            if (isConnected)
            {
                netClient?.Disconnect();
                isConnected = false;
                Console.WriteLine("🔌 Disconnected from server");
            }
        }
    }
} 