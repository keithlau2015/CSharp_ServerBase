# 🎮 Unity Integration Guide for C# Game Server

This guide shows you how to integrate the C# Game Server with Unity, allowing Unity to launch and control the server executable.

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
├── StreamingAssets/
│   └── GameServer.exe (or GameServer for Linux/Mac)
└── ...
```

### 3. Add GameServerManager Script

1. Copy `GameServerManager.cs` to your Unity project's `Scripts/` folder
2. Create an empty GameObject in your scene
3. Add the `GameServerManager` component to it
4. Configure the settings in the Inspector

## 📋 Configuration

### GameServerManager Inspector Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Server Executable Path** | Path to the server exe | `GameServer.exe` |
| **Server Port** | Port for the server | `8080` |
| **Max Players** | Maximum concurrent players | `100` |
| **Database Type** | Database backend | `EncryptedBinary` |
| **Data Directory** | Where to store data | `./GameData` |
| **Encryption Key** | Key for encrypted database | `UnityGameServerKey2024!` |
| **Auto Start On Awake** | Start server automatically | `true` |
| **Auto Stop On Destroy** | Stop server on cleanup | `true` |
| **Health Check Interval** | How often to check server | `5.0` seconds |
| **Show Server Console** | Display server console window | `true` |

## 💻 Usage Examples

### Basic Server Management

```csharp
using UnityEngine;
using GameServer.Unity;

public class GameManager : MonoBehaviour
{
    private GameServerManager serverManager;

    void Start()
    {
        // Get reference to server manager
        serverManager = FindObjectOfType<GameServerManager>();
        
        // Subscribe to events
        serverManager.OnServerStarted += OnServerStarted;
        serverManager.OnServerStopped += OnServerStopped;
        serverManager.OnServerError += OnServerError;
    }

    private void OnServerStarted()
    {
        Debug.Log("🎮 Server is ready! Players can now connect.");
        
        // Enable multiplayer UI
        EnableMultiplayerUI();
        
        // Start connecting clients
        ConnectToServer();
    }

    private void OnServerStopped()
    {
        Debug.Log("❌ Server has stopped.");
        
        // Disable multiplayer UI
        DisableMultiplayerUI();
    }

    private void OnServerError(string error)
    {
        Debug.LogError($"Server Error: {error}");
        
        // Show error message to user
        ShowErrorMessage($"Server failed: {error}");
    }

    // Manual server control
    public void StartServer()
    {
        serverManager.StartServer();
    }

    public void StopServer()
    {
        serverManager.StopServer();
    }

    public void RestartServer()
    {
        serverManager.RestartServer();
    }
}
```

### Advanced Server Integration

```csharp
using UnityEngine;
using UnityEngine.UI;
using GameServer.Unity;
using System.Threading.Tasks;

public class ServerControlPanel : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button stopButton;
    public Button restartButton;
    public Text statusText;
    public Text playerCountText;
    public InputField portInput;
    public InputField maxPlayersInput;

    private GameServerManager serverManager;

    void Start()
    {
        serverManager = FindObjectOfType<GameServerManager>();
        
        // Setup UI events
        startButton.onClick.AddListener(StartServer);
        stopButton.onClick.AddListener(StopServer);
        restartButton.onClick.AddListener(RestartServer);
        
        // Subscribe to server events
        serverManager.OnServerStarted += UpdateUI;
        serverManager.OnServerStopped += UpdateUI;
        serverManager.OnServerError += OnServerError;
        
        // Update UI initially
        UpdateUI();
        
        // Start health monitoring
        StartCoroutine(MonitorServerHealth());
    }

    private void StartServer()
    {
        // Update configuration from UI
        if (int.TryParse(portInput.text, out int port))
        {
            // You would need to expose these properties in GameServerManager
            // serverManager.ServerPort = port;
        }

        if (int.TryParse(maxPlayersInput.text, out int maxPlayers))
        {
            // serverManager.MaxPlayers = maxPlayers;
        }

        serverManager.StartServer();
    }

    private void StopServer()
    {
        serverManager.StopServer();
    }

    private void RestartServer()
    {
        serverManager.RestartServer();
    }

    private void UpdateUI()
    {
        bool isRunning = serverManager.IsServerRunning;
        
        startButton.interactable = !isRunning;
        stopButton.interactable = isRunning;
        restartButton.interactable = isRunning;
        
        statusText.text = isRunning ? "🟢 Running" : "🔴 Stopped";
        statusText.color = isRunning ? Color.green : Color.red;
        
        if (isRunning)
        {
            statusText.text += $" (Port: {serverManager.ServerPort})";
        }
    }

    private void OnServerError(string error)
    {
        statusText.text = $"❌ Error: {error}";
        statusText.color = Color.red;
    }

    private System.Collections.IEnumerator MonitorServerHealth()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            
            if (serverManager.IsServerRunning)
            {
                // Check if server is responsive
                CheckServerHealth();
            }
        }
    }

    private async void CheckServerHealth()
    {
        bool isResponding = await serverManager.IsServerResponding();
        
        if (!isResponding && serverManager.IsServerRunning)
        {
            statusText.text = "⚠️ Server Not Responding";
            statusText.color = Color.yellow;
        }
    }
}
```

## 🔧 Command Line Arguments

The server supports these command line arguments:

| Argument | Short | Description | Default |
|----------|-------|-------------|---------|
| `--port` | `-p` | Server port | `8080` |
| `--maxplayers` | `-m` | Maximum players | `100` |
| `--database` | `-db` | Database type | `EncryptedBinary` |
| `--datadir` | `-d` | Data directory | `./GameData` |
| `--key` | `-k` | Encryption key | (auto-generated) |
| `--noautostart` | | Don't auto-start scheduler | |
| `--help` | `-h` | Show help | |

### Example Command Line
```bash
GameServer.exe --port 8080 --maxplayers 500 --database EncryptedBinary --datadir "./ServerData"
```

## 🌐 Network Communication

### Client Connection Example

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GameClient : MonoBehaviour
{
    private GameServerManager serverManager;
    private string serverUrl;

    void Start()
    {
        serverManager = FindObjectOfType<GameServerManager>();
        
        // Wait for server to start
        serverManager.OnServerStarted += ConnectToServer;
    }

    private void ConnectToServer()
    {
        serverUrl = $"http://{serverManager.ServerAddress}";
        
        // Start connecting to the server
        StartCoroutine(ConnectToServerCoroutine());
    }

    private IEnumerator ConnectToServerCoroutine()
    {
        // Wait a moment for server to fully initialize
        yield return new WaitForSeconds(2f);
        
        // Test connection
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/health"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Successfully connected to server!");
                OnConnectedToServer();
            }
            else
            {
                Debug.LogError($"❌ Failed to connect to server: {request.error}");
                OnConnectionFailed();
            }
        }
    }

    private void OnConnectedToServer()
    {
        // Enable game UI
        // Start game logic
        Debug.Log("Game client connected and ready!");
    }

    private void OnConnectionFailed()
    {
        // Show error message
        // Retry connection or fallback to offline mode
        Debug.LogError("Failed to connect to game server");
    }

    // Example API call to server
    public IEnumerator SendPlayerAction(string action, string data)
    {
        string json = JsonUtility.ToJson(new { action = action, data = data });
        
        using (UnityWebRequest request = UnityWebRequest.Post($"{serverUrl}/player/action", json, "application/json"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Action sent: {action}");
                // Handle response
                string response = request.downloadHandler.text;
                Debug.Log($"Server response: {response}");
            }
            else
            {
                Debug.LogError($"Failed to send action: {request.error}");
            }
        }
    }
}
```

## 📱 Platform Considerations

### Windows
- Use `.exe` executable
- Server console window can be shown/hidden
- Antivirus may flag the executable (add exception)

### Mac
- Use the macOS executable (no extension)
- May need to grant permission in Security & Privacy settings
- Use `chmod +x GameServer` to make executable

### Linux
- Use the Linux executable (no extension)  
- Make sure it's executable: `chmod +x GameServer`
- May need to install additional dependencies

### Mobile (iOS/Android)
- Servers cannot run on mobile platforms
- Use this for development/testing only
- Consider cloud hosting for mobile games

## 🔒 Security Considerations

### Local Development
- Encryption key should be unique per project
- Don't commit encryption keys to version control
- Use different keys for development/production

### Production
- Don't distribute server executables with games
- Use dedicated server hosting
- Implement proper authentication

## 🐛 Troubleshooting

### Common Issues

**Server Won't Start**
- Check if executable exists at specified path
- Verify file permissions (executable on Linux/Mac)
- Check if port is already in use
- Look at Unity console for error messages

**Connection Failed**
- Verify server is actually running
- Check firewall settings
- Ensure correct IP/port configuration
- Test with basic HTTP request

**Server Crashes**
- Check server console output
- Look at server logs
- Verify system requirements (.NET 6.0)
- Check available disk space for database

**Performance Issues**
- Monitor CPU/memory usage
- Adjust max players setting
- Use optimized database settings
- Consider server hardware requirements

### Debug Commands

```csharp
// In Unity Console, you can test these:
GameServerManager serverManager = FindObjectOfType<GameServerManager>();

// Check server status
Debug.Log($"Server Running: {serverManager.IsServerRunning}");
Debug.Log($"Server Address: {serverManager.ServerAddress}");

// Manual server control
serverManager.StartServer();
serverManager.StopServer();
serverManager.RestartServer();
```

## 📈 Performance Tips

1. **Development Mode**: Show server console for debugging
2. **Production Mode**: Hide server console for better performance
3. **Database**: Use `OptimizedEncryptedBinary` for best local performance
4. **Health Checks**: Set appropriate intervals (not too frequent)
5. **Auto-Start**: Enable for seamless user experience

## 🎯 Best Practices

1. **Always** handle server startup/shutdown events
2. **Validate** server is running before attempting connections
3. **Provide** user feedback during server operations
4. **Handle** errors gracefully with user-friendly messages
5. **Test** on target platforms before release
6. **Consider** fallback options if server fails to start

---

**Ready to integrate your Unity game with the C# server?** 🚀

1. Build the server executable
2. Copy to Unity project
3. Add GameServerManager script
4. Configure settings
5. Test and deploy! 