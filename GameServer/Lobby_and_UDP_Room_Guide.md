# 🎮 Lobby System & UDP Room Broadcasting Guide

A comprehensive guide to using the multiplayer lobby system and real-time UDP broadcasting features for game rooms.

## 🚀 Overview

The lobby system provides:
- **Room Management**: Create, join, and manage game rooms
- **Player Management**: Track players across rooms with state management
- **Real-time Communication**: UDP broadcasting for position updates and actions
- **Event-driven Architecture**: React to lobby events in real-time
- **Scalable Design**: Handle multiple rooms and players efficiently

## 🏗️ Architecture

```
LobbyManager (Singleton)
├── Room Management
│   ├── GameRoom (Individual rooms)
│   └── Player Management per room
├── Broadcasting
│   ├── TCP (Reliable messages)
│   └── UDP (Real-time data)
└── Event System
    ├── Room Events
    └── Player Events
```

## 📋 Quick Start

### 1. Server Setup

The lobby system is automatically initialized when the server starts:

```csharp
// The LobbyManager is a singleton - access it anywhere
var lobbyManager = LobbyManager.Instance;

// Subscribe to events (optional)
lobbyManager.OnRoomCreated += (room) => Console.WriteLine($"Room created: {room.Name}");
lobbyManager.OnPlayerJoinedRoom += (player, room) => Console.WriteLine($"{player.Name} joined {room.Name}");
```

### 2. Client Connection

```csharp
// Example client connecting to server
var client = new ClientExample();
await client.ConnectToServer("127.0.0.1", 8080);
```

### 3. Basic Room Operations

```csharp
// Create a room
await client.CreateRoom("My Game Room", 4, false);

// Join a room
await client.JoinRoom("room-id", "PlayerName");

// Send real-time position updates (UDP)
await client.SendPositionUpdate(10.5f, 0f, 5.2f);

// Send player actions (UDP)
await client.SendPlayerAction("jump");
```

## 🏠 Room Management

### Creating Rooms

```csharp
// Server-side: Create room programmatically
var room = LobbyManager.Instance.CreateRoom(
    roomName: "Battle Arena",
    maxPlayers: 8,
    isPrivate: false,
    password: ""
);

// Client-side: Send create room request
var request = new CreateRoomRequest
{
    RoomName = "My Room",
    MaxPlayers = 4,
    IsPrivate = false,
    Password = ""
};

var packet = new Packet("CreateRoomRequest");
packet.Write(request);
netClient.Send(packet);
```

### Room States

```csharp
public enum GameRoomState
{
    Waiting,    // Waiting for players
    Starting,   // Game is starting
    InProgress, // Game is running
    Paused,     // Game is paused
    Finished    // Game has ended
}
```

### Room Properties

```csharp
// Check room status
bool isFull = room.IsFull;
bool isEmpty = room.IsEmpty;
bool gameStarted = room.IsGameStarted;
int playerCount = room.PlayerCount;
GameRoomState state = room.State;

// Room settings
room.SetCustomSetting("gameMode", "deathmatch");
string gameMode = room.GetCustomSetting<string>("gameMode");
```

## 👥 Player Management

### Player Properties

```csharp
public class Player
{
    public string UID { get; }
    public string Name { get; set; }
    public string CurrentRoomId { get; set; }
    public bool IsReady { get; set; }
    public PlayerPosition Position { get; set; }
    public PlayerStats Stats { get; set; }
    public PlayerState State { get; set; }
}
```

### Player Operations

```csharp
// Server-side: Get player info
var player = LobbyManager.Instance.GetPlayer(playerUID);
player.SetReady(true);
player.UpdatePosition(x, y, z);
player.IncrementKills();
player.AddScore(100);

// Client-side: Set ready state
var readyRequest = new PlayerReadyRequest { IsReady = true };
var packet = new Packet("PlayerReadyRequest");
packet.Write(readyRequest);
netClient.Send(packet);
```

## 🌐 Real-time UDP Communication

### Position Updates

Real-time position updates use UDP for low latency:

```csharp
// Client sends position update
public async Task SendPositionUpdate(float x, float y, float z, float rotX = 0, float rotY = 0, float rotZ = 0, float rotW = 1)
{
    var positionUpdate = new PlayerPositionUpdate
    {
        X = x, Y = y, Z = z,
        RotationX = rotX, RotationY = rotY, RotationZ = rotZ, RotationW = rotW,
        VelocityX = 0, VelocityY = 0, VelocityZ = 0
    };

    var packet = new Packet("PlayerPositionUpdate");
    packet.Write(positionUpdate);
    netClient.Send(packet, NetClient.SupportProtocol.UDP);
}

// Server broadcasts to all players in room (except sender)
LobbyManager.Instance.BroadcastToRoomUDP(
    roomId, 
    "PlayerPositionBroadcast", 
    broadcastData,
    excludePlayer: playerUID
);
```

### Player Actions

```csharp
// Common action types
string[] actionTypes = { "jump", "shoot", "attack", "interact", "death", "kill" };

// Send action
var action = new PlayerAction
{
    PlayerId = playerId,
    ActionType = "shoot",
    ActionData = "weapon_id_123",
    Timestamp = DateTime.UtcNow
};

var packet = new Packet("PlayerAction");
packet.Write(action);
netClient.Send(packet, NetClient.SupportProtocol.UDP);
```

### Ping/Latency Monitoring

```csharp
// Client sends ping
var pingRequest = new PingRequest { Timestamp = DateTime.UtcNow };
var packet = new Packet("PingRequest");
packet.Write(pingRequest);
netClient.Send(packet, NetClient.SupportProtocol.UDP);

// Server responds with pong
// Client calculates RTT: (pongReceived - pingRequest.Timestamp)
```

## 📡 Broadcasting System

### TCP Broadcasting (Reliable)

Use for important game events that must be delivered:

```csharp
// Room-wide announcements
LobbyManager.Instance.BroadcastToRoom(
    roomId,
    "GameStartedBroadcast",
    gameStartMessage
);

// Chat messages
LobbyManager.Instance.BroadcastToRoom(
    roomId,
    "ChatMessageBroadcast",
    chatMessage,
    excludePlayer: senderUID
);
```

### UDP Broadcasting (Fast)

Use for real-time data that can tolerate packet loss:

```csharp
// Position updates
LobbyManager.Instance.BroadcastToRoomUDP(
    roomId,
    "PlayerPositionBroadcast",
    positionData,
    excludePlayer: playerUID
);

// Real-time actions
LobbyManager.Instance.BroadcastToRoomUDP(
    roomId,
    "PlayerActionBroadcast",
    actionData,
    excludePlayer: playerUID
);
```

## 🎯 Game Loop Integration

### Typical Game Flow

```csharp
// 1. Players join lobby
// 2. Create or join rooms
// 3. Players set ready state
// 4. Start game
// 5. Real-time gameplay with UDP updates
// 6. End game and return to lobby

public async Task GameLoop()
{
    // Lobby phase
    var room = LobbyManager.Instance.CreateRoom("Battle Room", 4);
    
    // Wait for players to join and be ready
    while (!AllPlayersReady(room))
    {
        await Task.Delay(100);
    }
    
    // Start game
    room.StartGame();
    
    // Game phase with real-time updates
    while (room.IsGameStarted)
    {
        // Process real-time updates
        // Handle player actions
        // Update game state
        await Task.Delay(16); // ~60 FPS
    }
    
    // End game
    room.EndGame();
}
```

### Event Scheduling Integration

```csharp
// Use EventScheduler for timed events
var scheduler = EventScheduler.Instance;

// Auto-start game when all players ready
scheduler.ScheduleOneTimeEvent(
    "AutoStartGame",
    () => StartGameIfReady(room),
    DateTime.Now.AddSeconds(30),
    EventPriority.Normal
);

// Periodic game state updates
scheduler.ScheduleRecurringEvent(
    "GameStateUpdate",
    () => BroadcastGameState(room),
    RecurrenceType.Seconds,
    1,
    EventPriority.Normal
);
```

## 📊 Monitoring & Statistics

### Player Statistics

```csharp
public class PlayerStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Score { get; set; }
    public int Level { get; set; } = 1;
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public float KDRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;
}
```

### Room Analytics

```csharp
// Track room usage
var roomInfo = room.GetRoomInfo();
Console.WriteLine($"Room: {roomInfo.Name}");
Console.WriteLine($"Players: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}");
Console.WriteLine($"Uptime: {DateTime.UtcNow - roomInfo.CreatedAt}");
Console.WriteLine($"Last Activity: {roomInfo.LastActivity}");
```

## 🛠️ Packet Handlers Reference

### Lobby Handlers (TCP)

| Handler | Purpose | Request Type | Response Type |
|---------|---------|--------------|---------------|
| `CreateRoomHandler` | Create new room | `CreateRoomRequest` | `CreateRoomResponse` |
| `JoinRoomHandler` | Join existing room | `JoinRoomRequest` | `JoinRoomResponse` |
| `LeaveRoomHandler` | Leave current room | `LeaveRoomRequest` | `LeaveRoomResponse` |
| `GetRoomListHandler` | Get available rooms | - | `GetRoomListResponse` |
| `PlayerReadyHandler` | Set player ready state | `PlayerReadyRequest` | Broadcast |
| `StartGameHandler` | Start game in room | `StartGameRequest` | `StartGameResponse` |
| `ChatMessageHandler` | Send chat message | `ChatMessage` | Broadcast |

### Gameplay Handlers (UDP Preferred)

| Handler | Purpose | Request Type | Broadcast Type |
|---------|---------|--------------|----------------|
| `PlayerPositionUpdateHandler` | Update player position | `PlayerPositionUpdate` | `PlayerPositionBroadcast` |
| `PlayerActionHandler` | Handle player actions | `PlayerAction` | `PlayerActionBroadcast` |
| `GameStateUpdateHandler` | Update game state | `GameStateUpdate` | `GameStateBroadcast` |
| `PingHandler` | Latency measurement | `PingRequest` | `PongResponse` |

## ⚡ Performance Optimization

### Update Frequencies

```csharp
// Recommended update rates
Position Updates:    20-60 Hz (every 16-50ms)
Player Actions:      As needed (event-driven)
Game State:          1-10 Hz (every 100-1000ms)
Chat Messages:       As needed (event-driven)
Ping Checks:         1 Hz (every 1000ms)
```

### Bandwidth Optimization

```csharp
// Use compression for large data
public class OptimizedPositionUpdate
{
    public short X { get; set; }    // Use fixed-point math
    public short Y { get; set; }
    public short Z { get; set; }
    public byte Rotation { get; set; } // Quantized rotation
}

// Delta compression for position updates
public class DeltaPositionUpdate
{
    public float DeltaX { get; set; }  // Only send changes
    public float DeltaY { get; set; }
    public float DeltaZ { get; set; }
}
```

### Memory Management

```csharp
// Regular cleanup of inactive rooms
scheduler.ScheduleRecurringEvent(
    "CleanupInactiveRooms",
    () => CleanupInactiveRooms(),
    RecurrenceType.Minutes,
    5,
    EventPriority.Low
);

private void CleanupInactiveRooms()
{
    var inactiveRooms = LobbyManager.Instance.GetAllRooms()
        .Where(r => r.IsEmpty && !r.IsPersistent)
        .Where(r => DateTime.UtcNow - r.LastActivity > TimeSpan.FromMinutes(10))
        .ToList();
        
    foreach (var room in inactiveRooms)
    {
        LobbyManager.Instance.DestroyRoom(room.Id);
    }
}
```

## 🔧 Debugging & Troubleshooting

### Common Issues

**Players not receiving position updates:**
```csharp
// Check if player is in a room
if (string.IsNullOrEmpty(player.CurrentRoomId))
{
    Debug.DebugUtility.WarningLog("Player not in any room");
}

// Check UDP connection
if (!netClient.udProtocol.isAlive)
{
    Debug.DebugUtility.ErrorLog("UDP connection not established");
}
```

**High latency/packet loss:**
```csharp
// Monitor network quality
scheduler.ScheduleRecurringEvent(
    "NetworkMonitoring",
    () => MonitorNetworkQuality(),
    RecurrenceType.Seconds,
    5,
    EventPriority.Low
);
```

**Room state synchronization issues:**
```csharp
// Periodic room state validation
private void ValidateRoomState(GameRoom room)
{
    foreach (var player in room.GetPlayers())
    {
        if (!player.IsConnected)
        {
            Debug.DebugUtility.WarningLog($"Removing disconnected player: {player.Name}");
            LobbyManager.Instance.LeaveRoom(player.UID, room.Id);
        }
    }
}
```

## 🚀 Advanced Features

### Custom Game Modes

```csharp
// Extend room with custom settings
room.SetCustomSetting("gameMode", "deathmatch");
room.SetCustomSetting("timeLimit", 300); // 5 minutes
room.SetCustomSetting("scoreLimit", 25);
room.SetCustomSetting("weaponSet", "modern");

// Game mode specific logic
string gameMode = room.GetCustomSetting<string>("gameMode");
switch (gameMode)
{
    case "deathmatch":
        HandleDeathmatch(room);
        break;
    case "teamBattle":
        HandleTeamBattle(room);
        break;
    case "racing":
        HandleRacing(room);
        break;
}
```

### Spectator Mode

```csharp
// Add spectator support
public enum PlayerState
{
    Idle,
    InLobby,
    InGame,
    Spectating,
    Disconnected
}

// Spectators receive updates but don't send position data
if (player.State == PlayerState.Spectating)
{
    // Only receive broadcasts, don't send position updates
    player.ChangeState(PlayerState.Spectating);
}
```

### Room Persistence

```csharp
// Persistent rooms don't get destroyed when empty
var persistentRoom = LobbyManager.Instance.CreateRoom("Training Ground", 10);
persistentRoom.IsPersistent = true;

// Tournament rooms with scheduled events
scheduler.ScheduleWeeklyEvent(
    "WeeklyTournament",
    () => CreateTournamentRoom(),
    DayOfWeek.Saturday,
    new TimeSpan(20, 0, 0), // 8 PM
    EventPriority.High
);
```

## 📝 Example Implementation

See `Demo/LobbyAndGameplayDemo.cs` for a complete working example that demonstrates:

- ✅ Room creation and management
- ✅ Player joining and leaving
- ✅ Real-time position broadcasting
- ✅ Player action handling
- ✅ Game state management
- ✅ Statistics tracking
- ✅ Event-driven architecture

Run the demo by starting the server - it will automatically execute after 2 seconds.

## 🎯 Best Practices

1. **Use UDP for real-time data** (positions, actions)
2. **Use TCP for reliable data** (chat, game state changes)
3. **Implement proper error handling** for network failures
4. **Monitor and limit update frequencies** to prevent spam
5. **Clean up inactive rooms and players** regularly
6. **Use events for loose coupling** between systems
7. **Validate all incoming data** for security
8. **Implement proper disconnect handling**

---

**Ready to build your multiplayer game?** 🚀

Start with the demo, then customize the lobby system for your specific game needs! 