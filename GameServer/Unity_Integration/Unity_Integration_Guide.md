# 🎮 Unity Integration Guide for C# Game Server

This guide shows you how to integrate the complete C# Game Server with Unity, featuring lobby system, VoIP, real-time networking, and admin console.

## 🚀 Quick Setup

### 1. Build the Server Executable

#### Windows
```bash
cd GameServer
build_executable.bat
```

#### Linux/Mac
```bash
cd GameServer
chmod +x build_executable.sh
./build_executable.sh
```

### 2. Copy Server to Unity Project

Copy the appropriate executable to your Unity project:

```
YourUnityProject/
├── Assets/
│   └── Scripts/
│       ├── GameServerManager.cs
│       ├── NetworkManager.cs
│       ├── UnityPacket.cs
│       └── UnityMainThreadDispatcher.cs
├── StreamingAssets/
│   └── GameServer.exe (or GameServer for Linux/Mac)
└── ...
```

### 3. Add GameServerManager Script

1. Copy `GameServerManager.cs` to your Unity project's `Scripts/` folder
2. Create an empty GameObject named "GameServerManager" in your scene
3. Add the `GameServerManager` component to it
4. Configure the settings in the Inspector

## 📋 Configuration

### GameServerManager Inspector Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Server Executable Path** | Path to server exe | `GameServer.exe` |
| **TCP Port** | Reliable data port | `439500` |
| **UDP Port** | Real-time data port | `539500` |
| **Max Players** | Maximum concurrent players | `1000` |
| **Server Name** | Display name for server | `Unity Game Server` |
| **Auto Start On Awake** | Start server automatically | `true` |
| **Auto Stop On Destroy** | Stop server on cleanup | `true` |

#### Debug Configuration
| Setting | Description | Default |
|---------|-------------|---------|
| **Enable Debug Mode** | Show detailed logs | `true` |
| **Debug Level** | Log verbosity | `Info` |
| **Run Demo** | Start with demo players | `true` |
| **Enable Admin Console** | Allow console commands | `true` |

#### VoIP Configuration
| Setting | Description | Default |
|---------|-------------|---------|
| **Enable VoIP** | Voice chat system | `true` |
| **VoIP Sample Rate** | Audio quality | `48000` |
| **VoIP Codec** | Audio compression | `Opus` |
| **VoIP 3D Positional** | Spatial audio | `true` |

#### Monitoring
| Setting | Description | Default |
|---------|-------------|---------|
| **Health Check Interval** | Server monitoring rate | `5.0` seconds |
| **Show Server Console** | Display console window | `true` |
| **Auto Restart** | Restart on crash | `false` |
| **Max Restart Attempts** | Auto-restart limit | `3` |

## 💻 Complete Unity Integration

### 1. GameServerManager Usage

```csharp
using UnityEngine;
using GameServer.Unity;

public class GameController : MonoBehaviour
{
    private GameServerManager serverManager;
    private NetworkManager networkManager;

    void Start()
    {
        // Get server manager
        serverManager = FindObjectOfType<GameServerManager>();
        
        // Subscribe to server events
        serverManager.OnServerStarted += OnServerStarted;
        serverManager.OnServerStopped += OnServerStopped;
        serverManager.OnServerError += OnServerError;
        
        // Get network manager for client connections
        networkManager = FindObjectOfType<NetworkManager>();
    }

    private void OnServerStarted()
    {
        Debug.Log("🎮 C# Game Server is ready!");
        Debug.Log($"🌐 TCP: {serverManager.TCPAddress}");
        Debug.Log($"🌐 UDP: {serverManager.UDPAddress}");
        
        // Auto-connect Unity client to the server
        StartCoroutine(ConnectToServerAfterDelay());
    }

    private System.Collections.IEnumerator ConnectToServerAfterDelay()
    {
        // Wait for server to fully initialize
        yield return new WaitForSeconds(3f);
        
        // Connect Unity client to the server
        if (networkManager != null)
        {
            networkManager.ConnectToServer();
        }
    }

    private void OnServerStopped()
    {
        Debug.Log("❌ Server has stopped");
        if (networkManager != null && networkManager.IsConnected)
        {
            networkManager.Disconnect();
        }
    }

    private void OnServerError(string error)
    {
        Debug.LogError($"Server Error: {error}");
    }

    // Server control methods
    public void StartServer() => serverManager.StartServer();
    public void StopServer() => serverManager.StopServer();
    public void RestartServer() => serverManager.RestartServer();
    
    // Admin commands
    public void SendAdminCommand(string command)
    {
        serverManager.SendAdminCommand(command);
    }
    
    public void KickPlayer(string playerName)
    {
        serverManager.SendAdminCommand($"kick {playerName}");
    }
    
    public void BanPlayer(string playerName, string duration = "1h")
    {
        serverManager.SendAdminCommand($"ban {playerName} {duration}");
    }
    
    public void GetPlayerList()
    {
        serverManager.SendAdminCommand("players");
    }
    
    public void GetRoomList()
    {
        serverManager.SendAdminCommand("rooms");
    }
}
```

### 2. Network Client Integration

```csharp
using UnityEngine;
using UnityClient;

public class UnityNetworkClient : MonoBehaviour
{
    [Header("Network Settings")]
    public string playerName = "UnityPlayer";
    public bool autoConnect = true;
    
    private NetworkManager networkManager;
    private GameServerManager serverManager;

    void Start()
    {
        // Setup network manager
        networkManager = FindObjectOfType<NetworkManager>();
        serverManager = FindObjectOfType<GameServerManager>();
        
        if (networkManager != null)
        {
            // Configure for local server
            networkManager.serverIP = "127.0.0.1";
            networkManager.tcpPort = serverManager.TCPPort;
            networkManager.udpPort = serverManager.UDPPort;
            networkManager.playerName = playerName;
            
            // Subscribe to network events
            NetworkManager.OnConnected += OnConnectedToServer;
            NetworkManager.OnDisconnected += OnDisconnectedFromServer;
            NetworkManager.OnPacketReceived += OnPacketReceived;
        }
    }

    private void OnConnectedToServer()
    {
        Debug.Log("🎮 Unity client connected to C# Game Server!");
        
        // Auto-join or create a room
        StartCoroutine(AutoJoinRoomAfterDelay());
    }

    private System.Collections.IEnumerator AutoJoinRoomAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        
        // Try to join an existing room or create one
        await networkManager.GetRoomList();
        
        // Create a test room
        await networkManager.CreateRoom("Unity Test Room", 8, false);
    }

    private void OnDisconnectedFromServer()
    {
        Debug.Log("🎮 Unity client disconnected from server");
    }

    private void OnPacketReceived(string packetType, object data)
    {
        Debug.Log($"📦 Received packet: {packetType}");
        
        switch (packetType)
        {
            case "CreateRoomResponse":
                Debug.Log("✅ Room created successfully!");
                break;
                
            case "GetRoomListResponse":
                Debug.Log("📋 Received room list");
                break;
                
            case "PlayerPositionBroadcast":
                HandlePlayerMovement(data);
                break;
                
            case "PlayerActionBroadcast":
                HandlePlayerAction(data);
                break;
                
            case "ChatMessage":
                HandleChatMessage(data);
                break;
        }
    }

    private void HandlePlayerMovement(object data)
    {
        // Update other players' positions in your game
        Debug.Log("📍 Player moved");
    }

    private void HandlePlayerAction(object data)
    {
        // Handle player actions (jump, shoot, etc.)
        Debug.Log("🎯 Player action");
    }

    private void HandleChatMessage(object data)
    {
        // Display chat message in UI
        Debug.Log("💬 Chat message received");
    }

    // Gameplay functions
    public async void SendPositionUpdate(Vector3 position, Quaternion rotation)
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.SendPositionUpdate(position, rotation, Vector3.zero);
        }
    }

    public async void SendPlayerAction(string action, string data = "")
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.SendPlayerAction(action, data);
        }
    }

    public async void SendChatMessage(string message)
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.SendChatMessage(message);
        }
    }

    // Lobby functions
    public async void CreateRoom(string roomName)
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.CreateRoom(roomName, 8);
        }
    }

    public async void JoinRoom(string roomId)
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.JoinRoom(roomId);
        }
    }

    public async void LeaveRoom()
    {
        if (networkManager != null && networkManager.IsConnected)
        {
            await networkManager.LeaveRoom();
        }
    }
}
```

### 3. Player Movement Integration

```csharp
using UnityEngine;

public class NetworkedPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float positionUpdateRate = 20f; // 20 Hz
    
    private Rigidbody rb;
    private UnityNetworkClient networkClient;
    private float lastPositionUpdate;
    private Vector3 lastSentPosition;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        networkClient = FindObjectOfType<UnityNetworkClient>();
    }
    
    void Update()
    {
        HandleMovement();
        HandleNetworkUpdates();
        HandleActions();
    }
    
    private void HandleMovement()
    {
        // Basic WASD movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
        
        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Abs(rb.velocity.y) < 0.1f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            networkClient?.SendPlayerAction("jump");
        }
    }
    
    private void HandleNetworkUpdates()
    {
        if (networkClient == null) return;
        
        // Send position updates at specified rate
        if (Time.time - lastPositionUpdate > 1f / positionUpdateRate)
        {
            // Only send if position changed significantly
            if (Vector3.Distance(transform.position, lastSentPosition) > 0.1f)
            {
                networkClient.SendPositionUpdate(transform.position, transform.rotation);
                lastSentPosition = transform.position;
            }
            
            lastPositionUpdate = Time.time;
        }
    }
    
    private void HandleActions()
    {
        // Mouse actions
        if (Input.GetMouseButtonDown(0)) // Left click = shoot
        {
            networkClient?.SendPlayerAction("shoot", "weapon_rifle");
        }
        
        if (Input.GetKeyDown(KeyCode.E)) // E = interact
        {
            networkClient?.SendPlayerAction("interact");
        }
        
        if (Input.GetKeyDown(KeyCode.F)) // F = use item
        {
            networkClient?.SendPlayerAction("use_item", "health_potion");
        }
    }
}
```

### 4. Complete UI System

```csharp
using UnityEngine;
using UnityEngine.UI;
using GameServer.Unity;

public class GameServerUI : MonoBehaviour
{
    [Header("Server Control")]
    public Button startServerButton;
    public Button stopServerButton;
    public Button restartServerButton;
    public Text serverStatusText;
    public Text connectionInfoText;
    
    [Header("Network Control")]
    public Button connectButton;
    public Button disconnectButton;
    public Text networkStatusText;
    
    [Header("Lobby Control")]
    public InputField roomNameInput;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button leaveRoomButton;
    public Button getRoomsButton;
    
    [Header("Chat")]
    public InputField chatInput;
    public Button sendChatButton;
    public Text chatDisplay;
    
    [Header("Admin Commands")]
    public InputField adminCommandInput;
    public Button sendCommandButton;
    public Text adminResponseText;
    
    private GameServerManager serverManager;
    private UnityNetworkClient networkClient;
    
    void Start()
    {
        serverManager = FindObjectOfType<GameServerManager>();
        networkClient = FindObjectOfType<UnityNetworkClient>();
        
        SetupUI();
        SetupEvents();
    }
    
    private void SetupUI()
    {
        // Server control
        startServerButton.onClick.AddListener(() => serverManager.StartServer());
        stopServerButton.onClick.AddListener(() => serverManager.StopServer());
        restartServerButton.onClick.AddListener(() => serverManager.RestartServer());
        
        // Network control
        connectButton.onClick.AddListener(() => FindObjectOfType<NetworkManager>()?.ConnectToServer());
        disconnectButton.onClick.AddListener(() => FindObjectOfType<NetworkManager>()?.Disconnect());
        
        // Lobby control
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        leaveRoomButton.onClick.AddListener(() => networkClient?.LeaveRoom());
        getRoomsButton.onClick.AddListener(() => FindObjectOfType<NetworkManager>()?.GetRoomList());
        
        // Chat
        sendChatButton.onClick.AddListener(SendChatMessage);
        chatInput.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SendChatMessage(); });
        
        // Admin commands
        sendCommandButton.onClick.AddListener(SendAdminCommand);
        adminCommandInput.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SendAdminCommand(); });
    }
    
    private void SetupEvents()
    {
        // Server events
        serverManager.OnServerStarted += () => {
            serverStatusText.text = "🟢 Server Running";
            connectionInfoText.text = $"TCP: {serverManager.TCPAddress}\nUDP: {serverManager.UDPAddress}";
        };
        
        serverManager.OnServerStopped += () => {
            serverStatusText.text = "🔴 Server Stopped";
            connectionInfoText.text = "";
        };
        
        serverManager.OnServerError += (error) => {
            serverStatusText.text = $"❌ Server Error: {error}";
        };
        
        // Network events
        NetworkManager.OnConnected += () => {
            networkStatusText.text = "🟢 Connected to Server";
        };
        
        NetworkManager.OnDisconnected += () => {
            networkStatusText.text = "🔴 Disconnected";
        };
        
        NetworkManager.OnPacketReceived += (packetType, data) => {
            if (packetType == "ChatMessage")
            {
                chatDisplay.text += $"\n{data}";
            }
        };
    }
    
    private void CreateRoom()
    {
        string roomName = roomNameInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            networkClient?.CreateRoom(roomName);
            roomNameInput.text = "";
        }
    }
    
    private void JoinRoom()
    {
        string roomId = roomNameInput.text;
        if (!string.IsNullOrEmpty(roomId))
        {
            networkClient?.JoinRoom(roomId);
            roomNameInput.text = "";
        }
    }
    
    private void SendChatMessage()
    {
        string message = chatInput.text;
        if (!string.IsNullOrEmpty(message))
        {
            networkClient?.SendChatMessage(message);
            chatInput.text = "";
        }
    }
    
    private void SendAdminCommand()
    {
        string command = adminCommandInput.text;
        if (!string.IsNullOrEmpty(command))
        {
            serverManager.SendAdminCommand(command);
            adminResponseText.text = $"Sent: {command}";
            adminCommandInput.text = "";
        }
    }
    
    void Update()
    {
        // Update UI states
        bool serverRunning = serverManager.IsServerRunning;
        startServerButton.interactable = !serverRunning;
        stopServerButton.interactable = serverRunning;
        restartServerButton.interactable = serverRunning;
        
        bool networkConnected = FindObjectOfType<NetworkManager>()?.IsConnected ?? false;
        connectButton.interactable = !networkConnected && serverRunning;
        disconnectButton.interactable = networkConnected;
        
        // Lobby controls
        createRoomButton.interactable = networkConnected;
        joinRoomButton.interactable = networkConnected;
        leaveRoomButton.interactable = networkConnected;
        getRoomsButton.interactable = networkConnected;
        
        // Chat and admin
        sendChatButton.interactable = networkConnected;
        sendCommandButton.interactable = serverRunning;
    }
}
```

## 🎯 Server Features Available

### 🏠 Lobby System
- **Room Management**: Create, join, leave rooms
- **Player Management**: Ready status, player limits
- **Real-time Updates**: Player join/leave notifications
- **Room Discovery**: List available rooms

### 🎤 VoIP System
- **3D Positional Audio**: Distance-based volume and stereo
- **Voice Activation**: Push-to-talk, voice activation, open mic
- **Audio Processing**: Noise reduction, auto gain control
- **Voice Controls**: Mute, deafen, volume controls

### 📡 Real-time Networking
- **TCP**: Reliable data (lobby, chat, important events)
- **UDP**: Real-time data (positions, actions, audio)
- **Broadcasting**: Room-based message distribution
- **Low Latency**: Optimized for real-time gameplay

### ⚙️ Admin Console
- **Player Management**: Kick, ban, unban players
- **Room Management**: View, close rooms
- **VoIP Control**: Mute/unmute players
- **Live Monitoring**: Real-time server statistics
- **Commands**: Full admin command system

## 🔧 Available Admin Commands

You can send these commands through Unity:

```csharp
// Player management
serverManager.SendAdminCommand("players");           // List all players
serverManager.SendAdminCommand("kick PlayerName");   // Kick player
serverManager.SendAdminCommand("ban PlayerName 1h"); // Ban for 1 hour
serverManager.SendAdminCommand("unban PlayerName");  // Remove ban

// Room management
serverManager.SendAdminCommand("rooms");             // List all rooms
serverManager.SendAdminCommand("room RoomId");       // Room details
serverManager.SendAdminCommand("closeroom RoomId");  // Close room

// VoIP management
serverManager.SendAdminCommand("voip");              // VoIP status
serverManager.SendAdminCommand("mute PlayerName");   // Mute player
serverManager.SendAdminCommand("unmute PlayerName"); // Unmute player

// Server control
serverManager.SendAdminCommand("info");              // Server info
serverManager.SendAdminCommand("status");            // Server status
serverManager.SendAdminCommand("stop");              // Shutdown server
```

## 📱 Platform Support

### ✅ Supported Platforms
- **Windows**: Full support with .exe
- **Mac**: Full support with native executable
- **Linux**: Full support with native executable
- **Unity Editor**: Full development support

### ❌ Not Supported
- **Mobile**: Servers cannot run on iOS/Android
- **WebGL**: No process spawning support
- **Consoles**: Platform restrictions

## 🐛 Troubleshooting

### Server Won't Start
```csharp
// Check executable path
Debug.Log(serverManager.GetServerExecutablePath());

// Verify ports aren't in use
// Default: TCP 439500, UDP 539500

// Check Unity console for detailed errors
```

### Connection Issues
```csharp
// Test server response
bool responding = await serverManager.IsServerResponding();
Debug.Log($"Server responding: {responding}");

// Check network manager configuration
NetworkManager nm = FindObjectOfType<NetworkManager>();
Debug.Log($"Connecting to: {nm.serverIP}:{nm.tcpPort}");
```

### Performance Issues
- Reduce `positionUpdateRate` from 20 Hz to 10 Hz
- Disable VoIP if not needed
- Set `showServerConsole = false` for better performance
- Use Release build of server executable

## 🚀 Production Deployment

### For Local Multiplayer
1. Use the Unity integration as-is
2. Players connect to host's IP address
3. Configure router port forwarding for TCP 439500 and UDP 539500

### For Dedicated Servers
1. Deploy server executable to cloud hosting
2. Update Unity NetworkManager with server IP
3. Remove GameServerManager from client builds
4. Use separate admin tools for server management

## 📚 Next Steps

1. **Test the Basic Setup**: Start with server control and connection
2. **Implement Lobby**: Add room creation and joining
3. **Add Real-time Features**: Position updates and actions
4. **Enable VoIP**: Voice chat integration
5. **Admin Integration**: Server management tools
6. **Production Optimization**: Performance tuning

This integration provides a complete multiplayer foundation with professional-grade features ready for production use! 