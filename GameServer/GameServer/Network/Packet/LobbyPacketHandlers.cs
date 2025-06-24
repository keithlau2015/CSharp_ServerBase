using System;
using System.Threading.Tasks;
using GameSystem.Lobby;

namespace Network
{
    #region Create Room Handler
    
    public class CreateRoomHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var request = packet.ReadObject<CreateRoomRequest>();
                
                // Create room
                var room = LobbyManager.Instance.CreateRoom(
                    request.RoomName, 
                    request.MaxPlayers, 
                    request.IsPrivate, 
                    request.Password
                );
                
                var response = new CreateRoomResponse
                {
                    Success = room != null,
                    RoomId = room?.Id,
                    Message = room != null ? "Room created successfully" : "Failed to create room"
                };
                
                var responsePacket = new Packet("CreateRoomResponse");
                responsePacket.Write(response);
                netClient.Send(responsePacket);
                
                Debug.DebugUtility.DebugLog($"Create room request from {netClient.UID}: {response.Message}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"CreateRoomHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Join Room Handler
    
    public class JoinRoomHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var request = packet.ReadObject<JoinRoomRequest>();
                
                // Get or create player
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player == null)
                {
                    player = LobbyManager.Instance.CreatePlayer(netClient, request.PlayerName);
                }
                
                bool success = false;
                string message = "";
                
                if (player != null)
                {
                    success = LobbyManager.Instance.JoinRoom(player.UID, request.RoomId, request.Password);
                    message = success ? "Joined room successfully" : "Failed to join room";
                }
                else
                {
                    message = "Failed to create player";
                }
                
                var response = new JoinRoomResponse
                {
                    Success = success,
                    RoomId = request.RoomId,
                    Message = message,
                    RoomInfo = success ? LobbyManager.Instance.GetRoom(request.RoomId)?.GetRoomInfo() : null
                };
                
                var responsePacket = new Packet("JoinRoomResponse");
                responsePacket.Write(response);
                netClient.Send(responsePacket);
                
                Debug.DebugUtility.DebugLog($"Join room request from {netClient.UID}: {message}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"JoinRoomHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Leave Room Handler
    
    public class LeaveRoomHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var request = packet.ReadObject<LeaveRoomRequest>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                bool success = false;
                string message = "";
                
                if (player != null)
                {
                    success = LobbyManager.Instance.LeaveRoom(player.UID, request.RoomId);
                    message = success ? "Left room successfully" : "Failed to leave room";
                }
                else
                {
                    message = "Player not found";
                }
                
                var response = new LeaveRoomResponse
                {
                    Success = success,
                    Message = message
                };
                
                var responsePacket = new Packet("LeaveRoomResponse");
                responsePacket.Write(response);
                netClient.Send(responsePacket);
                
                Debug.DebugUtility.DebugLog($"Leave room request from {netClient.UID}: {message}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"LeaveRoomHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Get Room List Handler
    
    public class GetRoomListHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var availableRooms = LobbyManager.Instance.GetAvailableRooms();
                var roomInfoList = new System.Collections.Generic.List<RoomInfo>();
                
                foreach (var room in availableRooms)
                {
                    roomInfoList.Add(room.GetRoomInfo());
                }
                
                var response = new GetRoomListResponse
                {
                    Rooms = roomInfoList
                };
                
                var responsePacket = new Packet("GetRoomListResponse");
                responsePacket.Write(response);
                netClient.Send(responsePacket);
                
                Debug.DebugUtility.DebugLog($"Room list request from {netClient.UID}: {roomInfoList.Count} rooms available");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"GetRoomListHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Player Ready Handler
    
    public class PlayerReadyHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var request = packet.ReadObject<PlayerReadyRequest>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null)
                {
                    player.SetReady(request.IsReady);
                    
                    // Broadcast to room
                    if (!string.IsNullOrEmpty(player.CurrentRoomId))
                    {
                        var broadcastMessage = new PlayerReadyBroadcast
                        {
                            PlayerId = player.UID,
                            PlayerName = player.Name,
                            IsReady = request.IsReady
                        };
                        
                        LobbyManager.Instance.BroadcastToRoom(
                            player.CurrentRoomId, 
                            "PlayerReadyBroadcast", 
                            broadcastMessage,
                            excludePlayer: player.UID
                        );
                    }
                }
                
                Debug.DebugUtility.DebugLog($"Player ready state from {netClient.UID}: {request.IsReady}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"PlayerReadyHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Start Game Handler
    
    public class StartGameHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var request = packet.ReadObject<StartGameRequest>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    var room = LobbyManager.Instance.GetRoom(player.CurrentRoomId);
                    if (room != null)
                    {
                        bool success = room.StartGame();
                        
                        if (success)
                        {
                            // Broadcast game started to all players in room
                            var gameStartMessage = new GameStartedBroadcast
                            {
                                RoomId = room.Id,
                                Message = "Game has started!"
                            };
                            
                            LobbyManager.Instance.BroadcastToRoom(
                                room.Id, 
                                "GameStartedBroadcast", 
                                gameStartMessage
                            );
                            
                            // Change all players state to InGame
                            foreach (var roomPlayer in room.GetPlayers())
                            {
                                roomPlayer.ChangeState(PlayerState.InGame);
                            }
                        }
                        
                        var response = new StartGameResponse
                        {
                            Success = success,
                            Message = success ? "Game started successfully" : "Failed to start game"
                        };
                        
                        var responsePacket = new Packet("StartGameResponse");
                        responsePacket.Write(response);
                        netClient.Send(responsePacket);
                    }
                }
                
                Debug.DebugUtility.DebugLog($"Start game request from {netClient.UID}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"StartGameHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
}

#region Request/Response Classes

[ProtoBuf.ProtoContract]
public class CreateRoomRequest
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomName { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public int MaxPlayers { get; set; } = 4;
    
    [ProtoBuf.ProtoMember(3)]
    public bool IsPrivate { get; set; } = false;
    
    [ProtoBuf.ProtoMember(4)]
    public string Password { get; set; } = "";
}

[ProtoBuf.ProtoContract]
public class CreateRoomResponse
{
    [ProtoBuf.ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string RoomId { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string Message { get; set; }
}

[ProtoBuf.ProtoContract]
public class JoinRoomRequest
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string Password { get; set; } = "";
}

[ProtoBuf.ProtoContract]
public class JoinRoomResponse
{
    [ProtoBuf.ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string RoomId { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public string Message { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public GameSystem.Lobby.RoomInfo RoomInfo { get; set; }
}

[ProtoBuf.ProtoContract]
public class LeaveRoomRequest
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomId { get; set; }
}

[ProtoBuf.ProtoContract]
public class LeaveRoomResponse
{
    [ProtoBuf.ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string Message { get; set; }
}

[ProtoBuf.ProtoContract]
public class GetRoomListResponse
{
    [ProtoBuf.ProtoMember(1)]
    public System.Collections.Generic.List<GameSystem.Lobby.RoomInfo> Rooms { get; set; }
}

[ProtoBuf.ProtoContract]
public class PlayerReadyRequest
{
    [ProtoBuf.ProtoMember(1)]
    public bool IsReady { get; set; }
}

[ProtoBuf.ProtoContract]
public class PlayerReadyBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public bool IsReady { get; set; }
}

[ProtoBuf.ProtoContract]
public class StartGameRequest
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomId { get; set; }
}

[ProtoBuf.ProtoContract]
public class StartGameResponse
{
    [ProtoBuf.ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string Message { get; set; }
}

[ProtoBuf.ProtoContract]
public class GameStartedBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string RoomId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string Message { get; set; }
}

#endregion 