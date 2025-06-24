using System;
using Network;

namespace GameSystem.Lobby
{
    public class Player
    {
        public string UID { get; }
        public string Name { get; set; }
        public NetClient NetClient { get; }
        public string CurrentRoomId { get; set; }
        public bool IsReady { get; set; } = false;
        public DateTime LastActivity { get; set; }
        public DateTime ConnectedAt { get; }
        
        // Game Position Data
        public PlayerPosition Position { get; set; } = new PlayerPosition();
        public PlayerStats Stats { get; set; } = new PlayerStats();
        
        // Player State
        public PlayerState State { get; set; } = PlayerState.Idle;
        
        public Player(string uid, string name, NetClient netClient)
        {
            UID = uid;
            Name = name;
            NetClient = netClient;
            ConnectedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }
        
        #region Position Updates
        
        public void UpdatePosition(float x, float y, float z, float rotX = 0, float rotY = 0, float rotZ = 0, float rotW = 1)
        {
            Position.X = x;
            Position.Y = y;
            Position.Z = z;
            Position.RotationX = rotX;
            Position.RotationY = rotY;
            Position.RotationZ = rotZ;
            Position.RotationW = rotW;
            Position.Timestamp = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }
        
        public void UpdateVelocity(float velocityX, float velocityY, float velocityZ)
        {
            Position.VelocityX = velocityX;
            Position.VelocityY = velocityY;
            Position.VelocityZ = velocityZ;
            Position.Timestamp = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }
        
        #endregion
        
        #region Player Actions
        
        public void SetReady(bool ready)
        {
            IsReady = ready;
            LastActivity = DateTime.UtcNow;
            Debug.DebugUtility.DebugLog($"Player {Name} ready state: {ready}");
        }
        
        public void ChangeState(PlayerState newState)
        {
            if (State != newState)
            {
                var oldState = State;
                State = newState;
                LastActivity = DateTime.UtcNow;
                Debug.DebugUtility.DebugLog($"Player {Name} state changed from {oldState} to {newState}");
            }
        }
        
        #endregion
        
        #region Stats
        
        public void IncrementKills()
        {
            Stats.Kills++;
            LastActivity = DateTime.UtcNow;
        }
        
        public void IncrementDeaths()
        {
            Stats.Deaths++;
            LastActivity = DateTime.UtcNow;
        }
        
        public void AddScore(int points)
        {
            Stats.Score += points;
            LastActivity = DateTime.UtcNow;
        }
        
        #endregion
        
        public bool IsConnected => NetClient?.isAlive ?? false;
        
        public TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - LastActivity;
        }
        
        public TimeSpan GetSessionTime()
        {
            return DateTime.UtcNow - ConnectedAt;
        }
    }
    
    public enum PlayerState
    {
        Idle,
        InLobby,
        InGame,
        Spectating,
        Disconnected
    }
    
    #region Data Classes
    
    [ProtoBuf.ProtoContract]
    public class PlayerPosition
    {
        [ProtoBuf.ProtoMember(1)]
        public float X { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public float Y { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public float Z { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public float RotationX { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public float RotationY { get; set; }
        
        [ProtoBuf.ProtoMember(6)]
        public float RotationZ { get; set; }
        
        [ProtoBuf.ProtoMember(7)]
        public float RotationW { get; set; } = 1f;
        
        [ProtoBuf.ProtoMember(8)]
        public float VelocityX { get; set; }
        
        [ProtoBuf.ProtoMember(9)]
        public float VelocityY { get; set; }
        
        [ProtoBuf.ProtoMember(10)]
        public float VelocityZ { get; set; }
        
        [ProtoBuf.ProtoMember(11)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    [ProtoBuf.ProtoContract]
    public class PlayerStats
    {
        [ProtoBuf.ProtoMember(1)]
        public int Kills { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public int Deaths { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public int Score { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public int Level { get; set; } = 1;
        
        [ProtoBuf.ProtoMember(5)]
        public float Health { get; set; } = 100f;
        
        [ProtoBuf.ProtoMember(6)]
        public float MaxHealth { get; set; } = 100f;
        
        public float KDRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;
    }
    
    [ProtoBuf.ProtoContract]
    public class PlayerAction
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string ActionType { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public string ActionData { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [ProtoBuf.ProtoMember(5)]
        public PlayerPosition Position { get; set; }
    }
    
    #endregion
} 