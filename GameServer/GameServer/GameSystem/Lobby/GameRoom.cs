using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameSystem.Lobby
{
    public class GameRoom
    {
        public string Id { get; }
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsPrivate { get; set; }
        public string Password { get; set; }
        public bool IsPersistent { get; set; } = false;
        public DateTime CreatedAt { get; }
        public DateTime LastActivity { get; set; }
        
        // Game State
        public bool IsGameStarted { get; private set; } = false;
        public GameRoomState State { get; private set; } = GameRoomState.Waiting;
        
        // Players
        private readonly ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>();
        private readonly object _lockObject = new object();
        
        // Room Settings
        public Dictionary<string, object> CustomSettings { get; } = new Dictionary<string, object>();
        
        // Events
        public event Action<Player> OnPlayerJoined;
        public event Action<Player> OnPlayerLeft;
        public event Action<GameRoomState> OnStateChanged;
        
        public GameRoom(string id, string name, int maxPlayers = 4, bool isPrivate = false, string password = "")
        {
            Id = id;
            Name = name;
            MaxPlayers = maxPlayers;
            IsPrivate = isPrivate;
            Password = password;
            CreatedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }
        
        #region Properties
        
        public int PlayerCount => _players.Count;
        public bool IsFull => PlayerCount >= MaxPlayers;
        public bool IsEmpty => PlayerCount == 0;
        
        #endregion
        
        #region Player Management
        
        public bool AddPlayer(Player player)
        {
            if (player == null || IsFull)
                return false;
                
            lock (_lockObject)
            {
                if (_players.TryAdd(player.UID, player))
                {
                    player.CurrentRoomId = Id;
                    LastActivity = DateTime.UtcNow;
                    
                    Debug.DebugUtility.DebugLog($"Player {player.Name} added to room {Name}");
                    OnPlayerJoined?.Invoke(player);
                    return true;
                }
            }
            
            return false;
        }
        
        public bool RemovePlayer(Player player)
        {
            if (player == null)
                return false;
                
            lock (_lockObject)
            {
                if (_players.TryRemove(player.UID, out var removedPlayer))
                {
                    removedPlayer.CurrentRoomId = null;
                    LastActivity = DateTime.UtcNow;
                    
                    Debug.DebugUtility.DebugLog($"Player {player.Name} removed from room {Name}");
                    OnPlayerLeft?.Invoke(removedPlayer);
                    
                    // Auto-pause game if too few players
                    if (IsGameStarted && PlayerCount < 2)
                    {
                        PauseGame();
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        
        public Player GetPlayer(string playerUID)
        {
            _players.TryGetValue(playerUID, out var player);
            return player;
        }
        
        public List<Player> GetPlayers()
        {
            return _players.Values.ToList();
        }
        
        public bool HasPlayer(string playerUID)
        {
            return _players.ContainsKey(playerUID);
        }
        
        #endregion
        
        #region Game State Management
        
        public bool StartGame()
        {
            if (IsGameStarted || PlayerCount < 2)
                return false;
                
            lock (_lockObject)
            {
                IsGameStarted = true;
                ChangeState(GameRoomState.InProgress);
                LastActivity = DateTime.UtcNow;
                
                Debug.DebugUtility.DebugLog($"Game started in room {Name}");
                return true;
            }
        }
        
        public bool EndGame()
        {
            if (!IsGameStarted)
                return false;
                
            lock (_lockObject)
            {
                IsGameStarted = false;
                ChangeState(GameRoomState.Finished);
                LastActivity = DateTime.UtcNow;
                
                Debug.DebugUtility.DebugLog($"Game ended in room {Name}");
                return true;
            }
        }
        
        public bool PauseGame()
        {
            if (!IsGameStarted)
                return false;
                
            lock (_lockObject)
            {
                ChangeState(GameRoomState.Paused);
                LastActivity = DateTime.UtcNow;
                
                Debug.DebugUtility.DebugLog($"Game paused in room {Name}");
                return true;
            }
        }
        
        public bool ResumeGame()
        {
            if (!IsGameStarted || State != GameRoomState.Paused)
                return false;
                
            lock (_lockObject)
            {
                ChangeState(GameRoomState.InProgress);
                LastActivity = DateTime.UtcNow;
                
                Debug.DebugUtility.DebugLog($"Game resumed in room {Name}");
                return true;
            }
        }
        
        public void ResetRoom()
        {
            lock (_lockObject)
            {
                IsGameStarted = false;
                ChangeState(GameRoomState.Waiting);
                LastActivity = DateTime.UtcNow;
                
                Debug.DebugUtility.DebugLog($"Room {Name} has been reset");
            }
        }
        
        private void ChangeState(GameRoomState newState)
        {
            if (State != newState)
            {
                var oldState = State;
                State = newState;
                Debug.DebugUtility.DebugLog($"Room {Name} state changed from {oldState} to {newState}");
                OnStateChanged?.Invoke(newState);
            }
        }
        
        #endregion
        
        #region Room Settings
        
        public void SetCustomSetting(string key, object value)
        {
            lock (_lockObject)
            {
                CustomSettings[key] = value;
                LastActivity = DateTime.UtcNow;
            }
        }
        
        public T GetCustomSetting<T>(string key, T defaultValue = default(T))
        {
            lock (_lockObject)
            {
                if (CustomSettings.TryGetValue(key, out var value) && value is T)
                {
                    return (T)value;
                }
                return defaultValue;
            }
        }
        
        public bool RemoveCustomSetting(string key)
        {
            lock (_lockObject)
            {
                return CustomSettings.Remove(key);
            }
        }
        
        #endregion
        
        #region Room Info
        
        public RoomInfo GetRoomInfo()
        {
            return new RoomInfo
            {
                Id = Id,
                Name = Name,
                PlayerCount = PlayerCount,
                MaxPlayers = MaxPlayers,
                IsPrivate = IsPrivate,
                IsGameStarted = IsGameStarted,
                State = State.ToString(),
                CreatedAt = CreatedAt,
                LastActivity = LastActivity,
                Players = GetPlayers().Select(p => new PlayerInfo
                {
                    UID = p.UID,
                    Name = p.Name,
                    IsReady = p.IsReady
                }).ToList()
            };
        }
        
        #endregion
    }
    
    public enum GameRoomState
    {
        Waiting,    // Waiting for players
        Starting,   // Game is starting
        InProgress, // Game is running
        Paused,     // Game is paused
        Finished    // Game has ended
    }
    
    #region Data Transfer Objects
    
    [ProtoBuf.ProtoContract]
    public class RoomInfo
    {
        [ProtoBuf.ProtoMember(1)]
        public string Id { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string Name { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public int PlayerCount { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public int MaxPlayers { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public bool IsPrivate { get; set; }
        
        [ProtoBuf.ProtoMember(6)]
        public bool IsGameStarted { get; set; }
        
        [ProtoBuf.ProtoMember(7)]
        public string State { get; set; }
        
        [ProtoBuf.ProtoMember(8)]
        public DateTime CreatedAt { get; set; }
        
        [ProtoBuf.ProtoMember(9)]
        public DateTime LastActivity { get; set; }
        
        [ProtoBuf.ProtoMember(10)]
        public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();
    }
    
    [ProtoBuf.ProtoContract]
    public class PlayerInfo
    {
        [ProtoBuf.ProtoMember(1)]
        public string UID { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string Name { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public bool IsReady { get; set; }
    }
    
    #endregion
} 