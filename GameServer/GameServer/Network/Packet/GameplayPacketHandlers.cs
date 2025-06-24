using System;
using System.Threading.Tasks;
using GameSystem.Lobby;

namespace Network
{
    #region Player Position Update Handler (UDP)
    
    public class PlayerPositionUpdateHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var positionUpdate = packet.ReadObject<PlayerPositionUpdate>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    // Update player position
                    player.UpdatePosition(
                        positionUpdate.X, positionUpdate.Y, positionUpdate.Z,
                        positionUpdate.RotationX, positionUpdate.RotationY, 
                        positionUpdate.RotationZ, positionUpdate.RotationW
                    );
                    
                    player.UpdateVelocity(
                        positionUpdate.VelocityX, positionUpdate.VelocityY, positionUpdate.VelocityZ
                    );
                    
                    // Broadcast position to other players in the room (UDP)
                    var broadcastData = new PlayerPositionBroadcast
                    {
                        PlayerId = player.UID,
                        Position = player.Position,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    LobbyManager.Instance.BroadcastToRoomUDP(
                        player.CurrentRoomId,
                        "PlayerPositionBroadcast",
                        broadcastData,
                        excludePlayer: player.UID
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"PlayerPositionUpdateHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Player Action Handler (UDP)
    
    public class PlayerActionHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var action = packet.ReadObject<PlayerAction>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    // Process the action based on type
                    await ProcessPlayerAction(player, action);
                    
                    // Broadcast action to other players in the room (UDP)
                    var broadcastAction = new PlayerActionBroadcast
                    {
                        PlayerId = player.UID,
                        PlayerName = player.Name,
                        ActionType = action.ActionType,
                        ActionData = action.ActionData,
                        Position = player.Position,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    LobbyManager.Instance.BroadcastToRoomUDP(
                        player.CurrentRoomId,
                        "PlayerActionBroadcast",
                        broadcastAction,
                        excludePlayer: player.UID
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"PlayerActionHandler error: {ex.Message}");
            }
        }
        
        private async Task ProcessPlayerAction(Player player, PlayerAction action)
        {
            switch (action.ActionType.ToLower())
            {
                case "attack":
                    // Process attack action
                    Debug.DebugUtility.DebugLog($"Player {player.Name} performed attack");
                    break;
                    
                case "jump":
                    // Process jump action
                    Debug.DebugUtility.DebugLog($"Player {player.Name} jumped");
                    break;
                    
                case "shoot":
                    // Process shoot action
                    Debug.DebugUtility.DebugLog($"Player {player.Name} shot");
                    break;
                    
                case "interact":
                    // Process interaction
                    Debug.DebugUtility.DebugLog($"Player {player.Name} interacted with: {action.ActionData}");
                    break;
                    
                case "death":
                    // Handle player death
                    player.IncrementDeaths();
                    Debug.DebugUtility.DebugLog($"Player {player.Name} died");
                    break;
                    
                case "kill":
                    // Handle player kill
                    player.IncrementKills();
                    player.AddScore(100);
                    Debug.DebugUtility.DebugLog($"Player {player.Name} got a kill");
                    break;
                    
                default:
                    Debug.DebugUtility.DebugLog($"Unknown action type: {action.ActionType}");
                    break;
            }
        }
    }
    
    #endregion
    
    #region Game State Update Handler
    
    public class GameStateUpdateHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var gameState = packet.ReadObject<GameStateUpdate>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    var room = LobbyManager.Instance.GetRoom(player.CurrentRoomId);
                    if (room != null)
                    {
                        // Broadcast game state to all players in room
                        var broadcastData = new GameStateBroadcast
                        {
                            RoomId = room.Id,
                            GameTime = gameState.GameTime,
                            Score = gameState.Score,
                            CustomData = gameState.CustomData,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        LobbyManager.Instance.BroadcastToRoom(
                            room.Id,
                            "GameStateBroadcast",
                            broadcastData
                        );
                        
                        Debug.DebugUtility.DebugLog($"Game state updated in room {room.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"GameStateUpdateHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Ping Handler (UDP)
    
    public class PingHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var pingRequest = packet.ReadObject<PingRequest>();
                
                // Send pong response immediately
                var pongResponse = new PongResponse
                {
                    ClientTimestamp = pingRequest.Timestamp,
                    ServerTimestamp = DateTime.UtcNow
                };
                
                var responsePacket = new Packet("PongResponse");
                responsePacket.Write(pongResponse);
                netClient.Send(responsePacket, NetClient.SupportProtocol.UDP);
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"PingHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Chat Message Handler
    
    public class ChatMessageHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var chatMessage = packet.ReadObject<ChatMessage>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    // Broadcast chat message to room
                    var broadcastMessage = new ChatMessageBroadcast
                    {
                        PlayerId = player.UID,
                        PlayerName = player.Name,
                        Message = chatMessage.Message,
                        MessageType = chatMessage.MessageType,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    LobbyManager.Instance.BroadcastToRoom(
                        player.CurrentRoomId,
                        "ChatMessageBroadcast",
                        broadcastMessage,
                        excludePlayer: player.UID
                    );
                    
                    Debug.DebugUtility.DebugLog($"Chat from {player.Name}: {chatMessage.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"ChatMessageHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
}

#region Data Classes

[ProtoBuf.ProtoContract]
public class PlayerPositionUpdate
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
}

[ProtoBuf.ProtoContract]
public class PlayerPositionBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public GameSystem.Lobby.PlayerPosition Position { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class PlayerActionBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string ActionType { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public string ActionData { get; set; }
    
    [ProtoBuf.ProtoMember(5)]
    public GameSystem.Lobby.PlayerPosition Position { get; set; }
    
    [ProtoBuf.ProtoMember(6)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class GameStateUpdate
{
    [ProtoBuf.ProtoMember(1)]
    public float GameTime { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public int Score { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string CustomData { get; set; }
}

[ProtoBuf.ProtoContract]
public class GameStateBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public float GameTime { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public int Score { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public string CustomData { get; set; }
    
    [ProtoBuf.ProtoMember(5)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class PingRequest
{
    [ProtoBuf.ProtoMember(1)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class PongResponse
{
    [ProtoBuf.ProtoMember(1)]
    public DateTime ClientTimestamp { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public DateTime ServerTimestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class ChatMessage
{
    [ProtoBuf.ProtoMember(1)]
    public string Message { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string MessageType { get; set; } = "general";
}

[ProtoBuf.ProtoContract]
public class ChatMessageBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string Message { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public string MessageType { get; set; }
    
    [ProtoBuf.ProtoMember(5)]
    public DateTime Timestamp { get; set; }
}

#endregion 