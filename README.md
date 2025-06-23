# 🎮 C# Game Server Base

A comprehensive, production-ready game server framework written in C# .NET 6.0. Designed for scalable multiplayer games with built-in database management, event scheduling, and robust architecture patterns.

[![.NET](https://img.shields.io/badge/.NET-6.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey.svg)](https://github.com/dotnet/core/blob/main/os-lifecycle-policy.md)

## 🚀 Features

### 🔐 **Database Management**
- **Multiple Database Support**: MongoDB and Encrypted Binary Storage
- **AES-256 Encryption**: Secure local data storage with encrypted binary files
- **In-Memory Caching**: Lightning-fast data access with automatic persistence
- **CRUD Operations**: Standardized Create, Read, Update, Delete operations
- **Auto-Save**: Configurable automatic data persistence

### ⏰ **Event Scheduling System**
- **Flexible Scheduling**: One-time, recurring, daily, weekly, and monthly events
- **Priority-Based Execution**: Critical, High, Normal, and Low priority queues
- **Async Support**: Both synchronous and asynchronous event execution
- **Thread-Safe Operations**: Concurrent execution with proper synchronization
- **Graceful Shutdown**: Automatic cleanup and data persistence

### 🌐 **Network Infrastructure**
- **Scalable Architecture**: Designed for multiplayer game servers
- **Connection Management**: Robust client connection handling
- **Protocol Support**: Extensible network protocol implementation
- **Firewall Configuration**: Automatic firewall and port management
- **Cross-Platform Networking**: Windows, Linux, and macOS support

### 🎯 **Game Systems**
- **Modular Design**: Pluggable game system architecture
- **Time Management**: Server time synchronization and utilities
- **Debug System**: Comprehensive logging and debugging tools

### 🔧 **Development Tools**
- **Singleton Pattern**: Thread-safe singleton implementations
- **Configuration Management**: JSON-based configuration system
- **Error Handling**: Robust exception handling and logging

## 📋 Requirements

- **.NET 6.0** or higher
- **Windows, Linux, or macOS**

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/CSharp_ServerBase.git
cd CSharp_ServerBase
```

### 2. Build the Project
```bash
cd GameServer
dotnet build
```

### 3. Run the Server
```bash
dotnet run
```

### 4. Basic Configuration
Update `Config/appconfig.json` with your settings:
```json
{
  "DatabaseType": "EncryptedBinary",
  "DataDirectory": "./GameData",
  "EncryptionKey": "YourSecureEncryptionKey123!",
  "ServerPort": 8080,
  "MaxPlayers": 1000
}
```

## 🏗️ Architecture

```
GameServer/
├── Database/           # Database management layer
│   ├── DatabaseManagerBase.cs
│   ├── MongoDBManager.cs
│   ├── EncryptedBinaryDBManager.cs
│   ├── OptimizedEncryptedDBManager.cs
│   └── DatabaseFactory.cs
├── Network/            # Network communication layer
├── GameSystem/         # Game logic and systems
├── Utility/            # Utility classes and tools
│   ├── EventScheduler.cs
│   ├── TimeManager.cs
│   └── Singleton.cs
├── Debug/              # Debugging and logging
└── Config/             # Configuration files
```

## 💾 Database Usage

### Encrypted Binary Database (Recommended)
```csharp
// Initialize encrypted database
var database = DatabaseFactory.CreateDatabase(
    DatabaseFactory.DatabaseType.OptimizedEncryptedBinary,
    "./GameData",
    "YourSecureEncryptionKey123!"
);

// Player data example
public class PlayerData
{
    public string UID { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public DateTime LastLogin { get; set; }
}

// CRUD operations
var player = new PlayerData { UID = "player123", Name = "John", Level = 10 };

// Create
await database.CRUD_Instance(DatabaseBase.Action.Create, "GameDB", player);

// Read
var readPlayer = new PlayerData { UID = "player123" };
await database.CRUD_Instance(DatabaseBase.Action.Read, "GameDB", readPlayer);

// Update
readPlayer.Level = 15;
await database.CRUD_Instance(DatabaseBase.Action.Update, "GameDB", readPlayer);

// Delete
await database.CRUD_Instance(DatabaseBase.Action.Delete, "GameDB", readPlayer);
```

### MongoDB Database
```csharp
// Initialize MongoDB
var database = DatabaseFactory.CreateDatabase(
    DatabaseFactory.DatabaseType.MongoDB,
    "mongodb://localhost:27017/gamedb"
);
```

## ⏰ Event Scheduling System

### Overview
The EventScheduler is a comprehensive job scheduling system for your game server that supports:
- ✅ One-time events
- ✅ Recurring events (seconds, minutes, hours, daily, weekly, monthly)
- ✅ Priority-based execution
- ✅ Async and sync operations
- ✅ Thread-safe operations
- ✅ Graceful shutdown handling

### Basic Usage
```csharp
var scheduler = EventScheduler.Instance;
scheduler.StartScheduleThread();

// Daily maintenance at 3:00 AM
scheduler.ScheduleDailyEvent(
    "DailyMaintenance",
    PerformMaintenance,
    new TimeSpan(3, 0, 0),
    EventPriority.High
);

// Auto-save every 5 minutes
scheduler.ScheduleRecurringEvent(
    "AutoSave",
    SavePlayerData,
    RecurrenceType.Minutes,
    5,
    EventPriority.Normal
);

// Execute immediately
scheduler.ExecuteImmediately(
    "EmergencyTask",
    HandleEmergency,
    EventPriority.Critical
);
```

### Event Types and Scheduling

#### One-Time Events
```csharp
// Execute in 5 minutes
var eventId = scheduler.ScheduleOneTimeEvent(
    "PlayerReward",
    () => GivePlayerReward(),
    DateTime.Now.AddMinutes(5),
    EventPriority.Normal
);

// Async version
var asyncId = scheduler.ScheduleOneTimeEventAsync(
    "DatabaseCleanup",
    async () => await CleanupDatabaseAsync(),
    DateTime.Now.AddHours(1),
    EventPriority.High
);
```

#### Recurring Events
```csharp
// Every 30 seconds
scheduler.ScheduleRecurringEvent(
    "Heartbeat",
    SendHeartbeat,
    RecurrenceType.Seconds,
    30,
    EventPriority.Low
);

// Every 5 minutes
scheduler.ScheduleRecurringEvent(
    "AutoSave",
    SavePlayerData,
    RecurrenceType.Minutes,
    5,
    EventPriority.Normal
);

// Every 6 hours
scheduler.ScheduleRecurringEvent(
    "Backup",
    CreateBackup,
    RecurrenceType.Hours,
    6,
    EventPriority.High
);
```

#### Daily Events
```csharp
// Daily at 3:00 AM
scheduler.ScheduleDailyEvent(
    "DailyMaintenance",
    PerformMaintenance,
    new TimeSpan(3, 0, 0), // 3:00 AM
    EventPriority.High
);

// Daily reset at midnight
scheduler.ScheduleDailyEvent(
    "DailyReset",
    ResetDailyContent,
    TimeSpan.Zero, // Midnight
    EventPriority.Critical
);
```

#### Weekly Events
```csharp
// Every Sunday at 4:00 AM
scheduler.ScheduleWeeklyEvent(
    "WeeklyMaintenance",
    PerformWeeklyMaintenance,
    DayOfWeek.Sunday,
    new TimeSpan(4, 0, 0),
    EventPriority.Critical
);

// Every Friday at 8:00 PM
scheduler.ScheduleWeeklyEvent(
    "WeekendEvent",
    StartWeekendEvent,
    DayOfWeek.Friday,
    new TimeSpan(20, 0, 0),
    EventPriority.Normal
);
```

### Event Management
```csharp
// Remove events
bool removed = scheduler.RemoveScheduledEvent(eventId);

// Enable/Disable events
scheduler.DisableEvent(eventId);
scheduler.EnableEvent(eventId);

// Get event information
var eventInfo = scheduler.GetScheduledEvent(eventId);
var allEvents = scheduler.GetAllScheduledEvents();
var criticalEvents = scheduler.GetEventsByPriority(EventPriority.Critical);

// Check scheduler status
bool isRunning = scheduler.IsRunning;
int eventCount = scheduler.ScheduledEventCount;
int pendingCount = scheduler.PendingImmediateEvents;
```

### Priority Levels

| Priority | Use Case | Execution Order |
|----------|----------|----------------|
| `Critical` | Server shutdown, emergency tasks | 1st |
| `High` | Maintenance, backups, important notifications | 2nd |
| `Normal` | Regular game events, player actions | 3rd |
| `Low` | Monitoring, statistics, non-critical tasks | 4th |

### Recurrence Types

| Type | Description | Example |
|------|-------------|---------|
| `None` | One-time execution | Event reminders |
| `Seconds` | Every X seconds | Heartbeat (30s) |
| `Minutes` | Every X minutes | Auto-save (5min) |
| `Hours` | Every X hours | Backup (6h) |
| `Daily` | Daily at specific time | Maintenance (3 AM) |
| `Weekly` | Weekly on specific day/time | Weekly reset (Sunday 4 AM) |
| `Monthly` | Monthly on specific day | Monthly statistics |

### Best Practices for Event Scheduling

#### 1. Use Appropriate Priorities
```csharp
// ❌ Wrong - using Critical for non-critical tasks
scheduler.ScheduleRecurringEvent("Stats", UpdateStats, RecurrenceType.Minutes, 1, EventPriority.Critical);

// ✅ Correct - using appropriate priority
scheduler.ScheduleRecurringEvent("Stats", UpdateStats, RecurrenceType.Minutes, 1, EventPriority.Low);
```

#### 2. Handle Exceptions in Event Actions
```csharp
// ✅ Good practice - wrap in try-catch
scheduler.ScheduleRecurringEvent("RiskyTask", () => {
    try 
    {
        PerformRiskyOperation();
    }
    catch (Exception ex)
    {
        Debug.DebugUtility.ErrorLog($"RiskyTask failed: {ex.Message}");
    }
}, RecurrenceType.Minutes, 5, EventPriority.Normal);
```

#### 3. Use Meaningful Names
```csharp
// ❌ Poor naming
scheduler.ScheduleOneTimeEvent("Event1", DoSomething, DateTime.Now.AddMinutes(5));

// ✅ Clear naming
scheduler.ScheduleOneTimeEvent("PlayerLoginBonus", GiveLoginBonus, DateTime.Now.AddMinutes(5));
```

#### 4. Clean Up Resources
```csharp
// Always stop and dispose when shutting down
public void Shutdown()
{
    scheduler.StopScheduleThread();
    scheduler.Dispose();
}
```

## 🎮 Game Server Integration

### Complete Server Example
```csharp
public class GameServer
{
    private EventScheduler _scheduler;
    private DatabaseBase _database;

    public async Task StartServer()
    {
        // Initialize database
        _database = DatabaseFactory.CreateDatabase(
            DatabaseFactory.DatabaseType.OptimizedEncryptedBinary,
            "./GameData",
            "SecureKey123!"
        );

        // Initialize scheduler
        _scheduler = EventScheduler.Instance;
        _scheduler.StartScheduleThread();

        // Schedule server events
        SetupServerEvents();

        // Start network services
        await StartNetworkServices();

        Console.WriteLine("🎮 Game Server Started Successfully!");
    }

    private void SetupServerEvents()
    {
        // Daily maintenance
        _scheduler.ScheduleDailyEvent("DailyMaintenance", 
            PerformMaintenance, new TimeSpan(3, 0, 0), EventPriority.High);

        // Auto-save every 5 minutes
        _scheduler.ScheduleRecurringEvent("AutoSave", 
            SaveAllData, RecurrenceType.Minutes, 5, EventPriority.Normal);

        // Weekly server reset
        _scheduler.ScheduleWeeklyEvent("WeeklyReset", 
            PerformWeeklyReset, DayOfWeek.Sunday, 
            new TimeSpan(4, 0, 0), EventPriority.Critical);
    }

    public async Task StopServer()
    {
        // Graceful shutdown
        _scheduler.StopScheduleThread();
        DatabaseFactory.SaveDatabaseOnShutdown(_database);
        _scheduler.Dispose();
        
        Console.WriteLine("✅ Server shutdown complete!");
    }
}
```

### Common Game Server Events

#### Maintenance Events
```csharp
// Daily maintenance at 3 AM
scheduler.ScheduleDailyEvent("DailyMaintenance", PerformDailyMaintenance, new TimeSpan(3, 0, 0), EventPriority.High);

// Database backup every 6 hours
scheduler.ScheduleRecurringEvent("DatabaseBackup", BackupDatabase, RecurrenceType.Hours, 6, EventPriority.High);
```

#### Player Data Events
```csharp
// Auto-save every 5 minutes
scheduler.ScheduleRecurringEvent("AutoSave", SaveAllPlayers, RecurrenceType.Minutes, 5, EventPriority.Normal);

// Clear inactive sessions every 30 minutes
scheduler.ScheduleRecurringEvent("ClearSessions", ClearInactiveSessions, RecurrenceType.Minutes, 30, EventPriority.Normal);
```

#### Game Content Events
```csharp
// Daily reset at 6 AM
scheduler.ScheduleDailyEvent("DailyReset", ResetDailyContent, new TimeSpan(6, 0, 0), EventPriority.Critical);

// World boss spawn every 2 hours
scheduler.ScheduleRecurringEvent("WorldBoss", SpawnWorldBoss, RecurrenceType.Hours, 2, EventPriority.Normal);
```

#### Monitoring Events
```csharp
// Performance monitoring every minute
scheduler.ScheduleRecurringEvent("Monitor", MonitorPerformance, RecurrenceType.Minutes, 1, EventPriority.Low);

// Server heartbeat every 30 seconds
scheduler.ScheduleRecurringEvent("Heartbeat", SendHeartbeat, RecurrenceType.Seconds, 30, EventPriority.Low);
```

## 🔥 Network & Unity Integration

### Standalone Executable for Unity
```bash
# Build cross-platform executables
build_executable.bat        # Windows
./build_executable.sh       # Linux/Mac

# Run with network configuration
GameServer.exe --configure-firewall --show-port-forwarding
```

### Unity Integration
```csharp
// Add GameServerManager to Unity GameObject
public class GameManager : MonoBehaviour
{
    private GameServerManager serverManager;

    void Start()
    {
        serverManager = FindObjectOfType<GameServerManager>();
        serverManager.OnServerStarted += OnServerReady;
        serverManager.StartServer(); // Launches GameServer.exe
    }
    
    private void OnServerReady()
    {
        Debug.Log("🎮 Server is ready for connections!");
        // Connect your game clients
    }
}
```

### Command Line Options
```bash
# Basic usage
GameServer.exe

# Network configuration
GameServer.exe --port 8080 --configure-firewall --show-port-forwarding

# Unity integration
GameServer.exe --port 8080 --maxplayers 500 --no-firewall

# Database selection
GameServer.exe --database EncryptedBinary --datadir "./GameData"
```

## 🔥 Network Troubleshooting Guide

### 🚨 Quick Solutions

#### Problem: "Port in Use" Error
```bash
# Check what's using the port
netstat -ano | findstr :8080

# Use automatic port detection
GameServer.exe --port 8080  # Will auto-switch if needed
```

#### Problem: "Administrator Required" Error
```bash
# Windows: Run as Administrator
Right-click GameServer.exe → "Run as administrator"

# Or use the admin launcher
run_server_admin.bat

# Linux/Mac: Use sudo
sudo ./GameServer
```

#### Problem: "Firewall Blocking" Error
```bash
# Automatic firewall configuration
GameServer.exe --configure-firewall

# Manual Windows firewall
GameServer.exe --show-port-forwarding
```

### 🔍 Detailed Diagnostics

#### Check Network Status
```bash
# Run diagnostics
GameServer.exe --help

# Check if port is available
telnet localhost 8080

# Check firewall status (Windows)
netsh advfirewall show allprofiles state
```

#### Get Network Information
```bash
# Show port forwarding instructions
GameServer.exe --show-port-forwarding

# Check local IP
ipconfig  # Windows
ifconfig  # Linux/Mac
```

### 🛠️ Platform-Specific Solutions

#### 🪟 Windows Solutions

##### Firewall Configuration
```bash
# Automatic (requires admin)
GameServer.exe --configure-firewall

# Manual Windows Firewall Steps:
# 1. Windows Security → Firewall & network protection
# 2. Advanced settings → Inbound Rules → New Rule
# 3. Port → TCP → 8080 → Allow
# 4. Repeat for Outbound Rules
```

##### Administrator Privileges
```batch
:: Check if running as admin
net session >nul 2>&1

:: Request admin automatically
powershell -Command "Start-Process 'GameServer.exe' -Verb RunAs"

:: Use the provided admin launcher
run_server_admin.bat
```

##### Port Issues
```cmd
:: Find process using port
netstat -ano | findstr :8080
tasklist /FI "PID eq 1234"

:: Kill process if needed
taskkill /PID 1234 /F

:: Use different port
GameServer.exe --port 9000
```

#### 🐧 Linux Solutions

##### Firewall Configuration (UFW)
```bash
# Enable UFW
sudo ufw enable

# Allow port
sudo ufw allow 8080/tcp

# Check status
sudo ufw status
```

##### Firewall Configuration (iptables)
```bash
# Allow port
sudo iptables -A INPUT -p tcp --dport 8080 -j ACCEPT

# Save rules (Ubuntu/Debian)
sudo iptables-save > /etc/iptables/rules.v4

# Save rules (CentOS/RHEL)
sudo service iptables save
```

##### Permissions
```bash
# Run with elevated privileges
sudo ./GameServer --configure-firewall

# Make executable
chmod +x GameServer

# Check port permissions
sudo netstat -tulpn | grep :8080
```

#### 🍎 macOS Solutions

##### Firewall Configuration
```bash
# Check firewall status
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# Add application to firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add GameServer
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp GameServer

# Manual: System Preferences → Security & Privacy → Firewall
```

##### Permissions
```bash
# Grant permission in Security settings
# System Preferences → Security & Privacy → General
# Click "Allow Anyway" when prompted

# Run with sudo
sudo ./GameServer --configure-firewall
```

### 🌐 Router/Port Forwarding

#### Automatic Instructions
```bash
# Get detailed port forwarding guide
GameServer.exe --show-port-forwarding
```

#### Manual Router Configuration

##### 1. Find Router IP
```bash
# Windows
ipconfig | findstr "Default Gateway"

# Linux/Mac
route -n | grep '^0.0.0.0'
```

##### 2. Access Router Admin Panel
- Open browser: `http://192.168.1.1` or `http://192.168.0.1`
- Login with router credentials
- Look for: Port Forwarding, Virtual Servers, or NAT

##### 3. Add Port Forwarding Rule
```
Service Name: GameServer
Protocol: TCP
External Port: 8080
Internal Port: 8080
Internal IP: [Your PC's local IP]
Enable: ✓
```

##### 4. Test External Connection
```bash
# Get external IP
curl ifconfig.me

# Test port (online tool)
# Visit: https://www.yougetsignal.com/tools/open-ports/
# Enter your external IP and port 8080
```

### 🔧 Unity Integration Fixes

#### GameServerManager Issues

##### Server Won't Start
```csharp
// Check executable path
Debug.Log($"Looking for server at: {GetServerExecutablePath()}");

// Check permissions
if (!File.Exists(serverExecutablePath))
{
    Debug.LogError("Server executable not found!");
}
```

##### Firewall Configuration in Unity
```csharp
// Enable firewall configuration
[SerializeField] private bool configureFirewall = true;

// Monitor server process
serverManager.OnServerError += (error) => {
    Debug.LogError($"Server Error: {error}");
    // Handle firewall/permission errors
};
```

#### Common Unity Errors

##### "Access Denied" Error
```csharp
// Solution: Run Unity as Administrator
// Or disable firewall configuration
configureFirewall = false;
```

##### "Port Already in Use" Error
```csharp
// Solution: Enable auto port detection
autoDetectPort = true;

// Or specify different port
serverPort = 9000;
```

### 🛡️ Security Considerations

#### Local Development
- ✅ Use firewall rules for specific ports
- ✅ Use localhost/LAN connections only
- ❌ Don't expose to internet without proper security

#### Port Forwarding Risks
- ⚠️ Only forward ports for trusted players
- ⚠️ Use strong encryption keys
- ⚠️ Monitor connection logs
- ⚠️ Close ports when not needed

#### Alternative Solutions
```bash
# VPN solutions (safer than port forwarding)
- Hamachi (LogMeIn)
- ZeroTier
- Tailscale

# Steam networking
- Steam Remote Play Together
- Steam Networking API

# Cloud hosting
- AWS GameLift
- Google Cloud Game Servers
- Azure PlayFab
```

### 🆘 Emergency Solutions

#### Server Won't Start at All
1. **Check .NET Runtime**: `dotnet --version`
2. **Verify executable**: File exists and has correct permissions
3. **Try different port**: `--port 9000`
4. **Disable firewall**: `--no-firewall`
5. **Run as admin**: Right-click → "Run as administrator"

#### Can't Connect from Other Machines
1. **Check local connection first**: `telnet 127.0.0.1 8080`
2. **Test LAN connection**: `telnet [local-ip] 8080`
3. **Verify firewall rules**: Windows Firewall settings
4. **Check router settings**: Port forwarding configuration
5. **Test with online tool**: Port checker websites

#### Performance Issues
1. **Reduce max players**: `--maxplayers 50`
2. **Use optimized database**: `--database OptimizedEncryptedBinary`
3. **Monitor resources**: Task Manager
4. **Check network latency**: `ping [server-ip]`
5. **Restart router**: Unplug for 30 seconds

### 🎯 Best Practices

#### Development
- ✅ Test locally first (127.0.0.1)
- ✅ Use LAN testing before internet
- ✅ Keep firewall logs enabled
- ✅ Document working configurations

#### Production
- ✅ Use dedicated server hosting
- ✅ Implement proper authentication
- ✅ Monitor server health
- ✅ Have backup/failover plans
- ❌ Don't use home internet for production

#### Troubleshooting Workflow
1. **Local test**: Can you connect to 127.0.0.1?
2. **LAN test**: Can other devices on your network connect?
3. **Firewall test**: Are rules configured correctly?
4. **Router test**: Is port forwarding working?
5. **Internet test**: Can external users connect?

## 🔧 Configuration

### Database Configuration
```csharp
// Choose your database type
DatabaseFactory.DatabaseType.MongoDB              // MongoDB
DatabaseFactory.DatabaseType.EncryptedBinary      // Basic encrypted binary
DatabaseFactory.DatabaseType.OptimizedEncryptedBinary // Optimized with auto-save
```

### Event Priorities
```csharp
EventPriority.Critical  // Server shutdown, emergency tasks
EventPriority.High     // Maintenance, backups, notifications
EventPriority.Normal   // Regular game events, player actions
EventPriority.Low      // Monitoring, statistics, non-critical tasks
```

## 🎯 Use Cases

### MMO Game Server
- Player data persistence with encryption
- Daily/weekly content resets
- Automated maintenance and backups
- Real-time event scheduling

### Battle Royale Server
- Match scheduling and management
- Player statistics tracking
- Server performance monitoring
- Automated scaling events

### RPG Game Server
- Quest and event management
- Player progression tracking
- World events and spawning
- Economy and item management

## 🛠️ Development

### Project Structure
```
GameServer/
├── GameServer/         # Main server application
│   ├── Database/       # Database management
│   ├── Network/        # Network layer
│   ├── GameSystem/     # Game logic
│   ├── Utility/        # Utilities and tools
│   └── Debug/          # Debugging tools
├── Config/             # Configuration files
└── GameServer.sln      # Visual Studio solution
```

### Dependencies
- **MongoDB.Driver** - MongoDB database support
- **Newtonsoft.Json** - JSON serialization
- **NLog** - Logging framework
- **protobuf-net** - Protocol buffer serialization

### Building from Source
```bash
git clone https://github.com/keithlau2015/CSharp_ServerBase.git
cd CSharp_ServerBase/GameServer
dotnet restore
dotnet build --configuration Release
```

## 🔒 Security Features

- **AES-256 Encryption** for local data storage
- **Secure key derivation** using SHA-256
- **Thread-safe operations** for concurrent access
- **Graceful shutdown** to prevent data corruption
- **Error handling** with comprehensive logging

## 📈 Performance

- **In-Memory Caching** for fast data access
- **Concurrent Collections** for thread-safe operations
- **Optimized Scheduling** with priority queues
- **Automatic Cleanup** of completed events
- **Lazy Initialization** for efficient resource usage

## 🚨 Error Handling and Troubleshooting

### EventScheduler Error Handling
```csharp
scheduler.ScheduleRecurringEvent("DatabaseOperation", () => {
    try 
    {
        PerformDatabaseOperation();
    }
    catch (DatabaseException ex)
    {
        Debug.DebugUtility.ErrorLog($"Database operation failed: {ex.Message}");
        // Maybe schedule a retry or switch to backup database
    }
    catch (Exception ex)
    {
        Debug.DebugUtility.ErrorLog($"Unexpected error: {ex.Message}");
    }
}, RecurrenceType.Minutes, 5, EventPriority.Normal);
```

### Common Issues

#### Events Not Executing
- Check if scheduler is started: `scheduler.StartScheduleThread()`
- Verify event is enabled: `scheduler.EnableEvent(eventId)`
- Check execution time: `scheduler.GetScheduledEvent(eventId).NextExecutionTime`

#### High CPU Usage
- Reduce frequency of recurring events
- Make event actions more efficient
- Use async operations for I/O bound tasks

#### Memory Leaks
- Ensure proper disposal: `scheduler.Dispose()`
- Remove unused events: `scheduler.RemoveScheduledEvent(eventId)`
- Clear all events if needed: `scheduler.ClearAllEvents()`

### Performance Considerations

1. **Event Frequency**: Don't schedule too many high-frequency events
2. **Event Duration**: Keep event actions short to avoid blocking
3. **Memory Usage**: Remove completed one-time events automatically
4. **Thread Safety**: All operations are thread-safe, but your event actions should be too

## 📊 Monitoring & Diagnostics

### Real-time Monitoring
```bash
# Monitor network connections
netstat -an | findstr :8080

# Monitor server logs
tail -f server.log

# Monitor system resources
Resource Monitor → Network tab
```

### Performance Diagnostics
```bash
# Check CPU/Memory usage
Task Manager → Processes → GameServer.exe

# Network performance
ping 127.0.0.1
iperf3 -s  # Server
iperf3 -c 127.0.0.1  # Client
```

### Built-in Diagnostics
```bash
# Run comprehensive diagnostics
GameServer.exe --help

# Network information
GameServer.exe --show-port-forwarding
```

### Log Files Location
```
Windows: %APPDATA%/GameServer/logs/
Linux:   ~/.local/share/GameServer/logs/
macOS:   ~/Library/Application Support/GameServer/logs/
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding standards
- Add unit tests for new features
- Update documentation
- Ensure thread safety for concurrent operations

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Keith** - *Initial work and architecture*

## 🙏 Acknowledgments

- Built with ❤️ for the game development community
- Inspired by modern game server architectures
- Designed for scalability and performance

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/your-username/CSharp_ServerBase/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-username/CSharp_ServerBase/discussions)

### Community Resources
- 🔗 [Port Checker Tool](https://www.yougetsignal.com/tools/open-ports/)
- 🔗 [What Is My IP](https://whatismyipaddress.com/)
- 🔗 [Windows Firewall Guide](https://support.microsoft.com/en-us/windows/turn-microsoft-defender-firewall-on-or-off-ec0844f7-aebd-0583-67fe-601ecf5d774f)

---

**Ready to build your next game server?** 🚀

```bash
git clone https://github.com/your-username/CSharp_ServerBase.git
cd CSharp_ServerBase/GameServer
dotnet run
```

Remember: Most network issues can be resolved with proper firewall configuration and administrator privileges! 🚀
