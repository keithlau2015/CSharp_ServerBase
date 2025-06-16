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

## ⏰ Event Scheduling

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

### Event Types
- **One-Time Events**: Execute once at specific time
- **Recurring Events**: Every X seconds/minutes/hours
- **Daily Events**: Daily at specific time (e.g., 3:00 AM)
- **Weekly Events**: Weekly on specific day/time
- **Monthly Events**: Monthly on specific day

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

## 📚 Documentation

- **[EventScheduler Documentation](GameServer/GameServer/Utility/EventScheduler_README.md)** - Comprehensive scheduling guide
- **[Database Examples](GameServer/GameServer/Database/DatabaseExample.cs)** - Database usage examples
- **[Server Integration](GameServer/GameServer/Utility/ServerStartupExample.cs)** - Complete server setup

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
git clone https://github.com/your-username/CSharp_ServerBase.git
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
- **Documentation**: Check the `/docs` folder for detailed guides

---

**Ready to build your next game server?** 🚀

```bash
git clone https://github.com/your-username/CSharp_ServerBase.git
cd CSharp_ServerBase/GameServer
dotnet run
```
