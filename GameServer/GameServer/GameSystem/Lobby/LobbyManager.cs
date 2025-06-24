using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Network;

namespace GameSystem.Lobby
{
    public class LobbyManager
    {
        private static LobbyManager _instance;
        public static LobbyManager Instance => _instance ??= new LobbyManager();

        private readonly ConcurrentDictionary<string, GameRoom> _rooms = new ConcurrentDictionary<string, GameRoom>();
        private readonly ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>();
        
        public event Action<GameRoom> OnRoomCreated;
        public event Action<string> OnRoomDestroyed;
        public event Action<Player, GameRoom> OnPlayerJoinedRoom;
        public event Action<Player, GameRoom> OnPlayerLeftRoom;

        private LobbyManager() { }

        #region Room Management

        public GameRoom CreateRoom(string roomName, int maxPlayers = 4, bool isPrivate = false, string password = "")
        {
            var roomId = Guid.NewGuid().ToString();
            var room = new GameRoom(roomId, roomName, maxPlayers, isPrivate, password);
            
            if (_rooms.TryAdd(roomId, room))
            {
                // Create voice channel for the room if VoIP is available
                try
                {
                    var voipManager = GameSystem.VoIP.VoIPManager.Instance;
                    voipManager.CreateVoiceChannel(roomId);
                    Debug.DebugUtility.DebugLog($"Voice channel created for room: {roomName}");
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.WarningLog($"VoIP not available: {ex.Message}");
                }
                
                Debug.DebugUtility.DebugLog($"Room created: {roomName} (ID: {roomId})");
                OnRoomCreated?.Invoke(room);
                return room;
            }
            
            Debug.DebugUtility.ErrorLog($"Failed to create room: {roomName}");
            return null;
        }

        public bool DestroyRoom(string roomId)
        {
            if (_rooms.TryRemove(roomId, out var room))
            {
                // Kick all players from the room
                var playersToKick = room.GetPlayers().ToList();
                foreach (var player in playersToKick)
                {
                    LeaveRoom(player.UID, roomId);
                }
                
                // Destroy voice channel if VoIP is available
                try
                {
                    var voipManager = GameSystem.VoIP.VoIPManager.Instance;
                    voipManager.DestroyVoiceChannel(roomId);
                    Debug.DebugUtility.DebugLog($"Voice channel destroyed for room: {room.Name}");
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.WarningLog($"VoIP cleanup failed: {ex.Message}");
                }
                
                Debug.DebugUtility.DebugLog($"Room destroyed: {room.Name} (ID: {roomId})");
                OnRoomDestroyed?.Invoke(roomId);
                return true;
            }
            
            return false;
        }

        public GameRoom GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public List<GameRoom> GetAvailableRooms()
        {
            return _rooms.Values
                .Where(room => !room.IsFull && !room.IsGameStarted && !room.IsPrivate)
                .ToList();
        }

        public List<GameRoom> GetAllRooms()
        {
            return _rooms.Values.ToList();
        }

        #endregion

        #region Player Management

        public Player CreatePlayer(NetClient netClient, string playerName)
        {
            var player = new Player(netClient.UID.ToString(), playerName, netClient);
            
            if (_players.TryAdd(player.UID, player))
            {
                Debug.DebugUtility.DebugLog($"Player created: {playerName} (UID: {player.UID})");
                return player;
            }
            
            Debug.DebugUtility.ErrorLog($"Failed to create player: {playerName}");
            return null;
        }

        public bool RemovePlayer(string playerUID)
        {
            if (_players.TryRemove(playerUID, out var player))
            {
                // Remove player from any room they're in
                if (!string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    LeaveRoom(playerUID, player.CurrentRoomId);
                }
                
                // Clean up VoIP state if available
                try
                {
                    var voipManager = GameSystem.VoIP.VoIPManager.Instance;
                    voipManager.RemovePlayer(playerUID);
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.WarningLog($"VoIP player cleanup failed: {ex.Message}");
                }
                
                Debug.DebugUtility.DebugLog($"Player removed: {player.Name} (UID: {playerUID})");
                return true;
            }
            
            return false;
        }

        public Player GetPlayer(string playerUID)
        {
            _players.TryGetValue(playerUID, out var player);
            return player;
        }

        public List<Player> GetAllPlayers()
        {
            return _players.Values.ToList();
        }

        #endregion

        #region Room Operations

        public bool JoinRoom(string playerUID, string roomId, string password = "")
        {
            var player = GetPlayer(playerUID);
            var room = GetRoom(roomId);
            
            if (player == null)
            {
                Debug.DebugUtility.ErrorLog($"Player not found: {playerUID}");
                return false;
            }
            
            if (room == null)
            {
                Debug.DebugUtility.ErrorLog($"Room not found: {roomId}");
                return false;
            }
            
            // Check if player is already in a room
            if (!string.IsNullOrEmpty(player.CurrentRoomId))
            {
                Debug.DebugUtility.WarningLog($"Player {player.Name} is already in room {player.CurrentRoomId}");
                return false;
            }
            
            // Check room capacity
            if (room.IsFull)
            {
                Debug.DebugUtility.WarningLog($"Room {room.Name} is full");
                return false;
            }
            
            // Check password for private rooms
            if (room.IsPrivate && room.Password != password)
            {
                Debug.DebugUtility.WarningLog($"Incorrect password for room {room.Name}");
                return false;
            }
            
            // Add player to room
            if (room.AddPlayer(player))
            {
                player.CurrentRoomId = roomId;
                Debug.DebugUtility.DebugLog($"Player {player.Name} joined room {room.Name}");
                OnPlayerJoinedRoom?.Invoke(player, room);
                
                // Notify other players in the room
                var joinMessage = new PlayerJoinedRoomMessage
                {
                    PlayerId = player.UID,
                    PlayerName = player.Name,
                    RoomId = roomId
                };
                
                BroadcastToRoom(roomId, "PlayerJoinedRoom", joinMessage, excludePlayer: player.UID);
                return true;
            }
            
            return false;
        }

        public bool LeaveRoom(string playerUID, string roomId)
        {
            var player = GetPlayer(playerUID);
            var room = GetRoom(roomId);
            
            if (player == null || room == null)
                return false;
            
            if (room.RemovePlayer(player))
            {
                player.CurrentRoomId = null;
                Debug.DebugUtility.DebugLog($"Player {player.Name} left room {room.Name}");
                OnPlayerLeftRoom?.Invoke(player, room);
                
                // Notify other players in the room
                var leaveMessage = new PlayerLeftRoomMessage
                {
                    PlayerId = player.UID,
                    PlayerName = player.Name,
                    RoomId = roomId
                };
                
                BroadcastToRoom(roomId, "PlayerLeftRoom", leaveMessage);
                
                // Destroy room if empty and not persistent
                if (room.PlayerCount == 0 && !room.IsPersistent)
                {
                    DestroyRoom(roomId);
                }
                
                return true;
            }
            
            return false;
        }

        #endregion

        #region Broadcasting

        public void BroadcastToRoom(string roomId, string messageType, object data, string excludePlayer = null)
        {
            var room = GetRoom(roomId);
            if (room == null) return;
            
            var packet = new Packet(messageType);
            packet.Write(data);
            
            foreach (var player in room.GetPlayers())
            {
                if (excludePlayer != null && player.UID == excludePlayer)
                    continue;
                
                player.NetClient?.Send(packet, NetClient.SupportProtocol.TCP);
            }
        }

        public void BroadcastToRoomUDP(string roomId, string messageType, object data, string excludePlayer = null)
        {
            var room = GetRoom(roomId);
            if (room == null) return;
            
            var packet = new Packet(messageType);
            packet.Write(data);
            
            foreach (var player in room.GetPlayers())
            {
                if (excludePlayer != null && player.UID == excludePlayer)
                    continue;
                
                player.NetClient?.Send(packet, NetClient.SupportProtocol.UDP);
            }
        }

        #endregion
    }

    #region Message Classes

    [ProtoBuf.ProtoContract]
    public class PlayerJoinedRoomMessage
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string PlayerName { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public string RoomId { get; set; }
    }

    [ProtoBuf.ProtoContract]
    public class PlayerLeftRoomMessage
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string PlayerName { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public string RoomId { get; set; }
    }

    #endregion
} 