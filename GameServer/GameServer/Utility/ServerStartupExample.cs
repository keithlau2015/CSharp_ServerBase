using System;
using System.Threading.Tasks;
using Database;
using Utility;
using GameServer.Examples;
using System.Threading;
using Network;  // Add this for Server access

namespace GameServer
{
    /// <summary>
    /// Complete server startup orchestrator that integrates all components:
    /// Database, EventScheduler, TCP/UDP Networking, and Admin Console
    /// </summary>
    public class ServerStartupExample
    {
        private EventScheduler _eventScheduler;
        private EventSchedulerExample _schedulerExample;
        private DatabaseBase _database;
        private CancellationTokenSource _cancellationTokenSource;
        private ServerConfig _config;
        private bool _isServerRunning = false;

        public async Task StartServer()
        {
            var defaultConfig = new ServerConfig
            {
                ID = 1,
                Name = "GameServer",
                Port = 8080,
                TCPPort = 8080,
                UDPPort = 8081,
                MaxPlayers = 100,
                DatabaseType = "EncryptedBinary",
                DataDirectory = "./GameData",
                EncryptionKey = "DefaultGameServerKey2024!",
                AutoStart = true,
                DebugLevel = 1
            };
            
            await StartServerWithConfig(defaultConfig);
        }

        public async Task StartServerWithConfig(ServerConfig config)
        {
            try
            {
                _config = config;
                Debug.DebugUtility.DebugLog("🚀 === Starting Complete Game Server ===");
                
                _cancellationTokenSource = new CancellationTokenSource();

                // 1. Initialize Database first
                InitializeDatabase();

                // 2. Initialize EventScheduler
                InitializeEventScheduler();

                // 3. Start the full TCP/UDP Server with all packet handlers
                Debug.DebugUtility.DebugLog("🌐 Starting TCP/UDP Server...");
                await Server.StartServerWithConfig(_config);

                // 4. Setup graceful shutdown
                SetupGracefulShutdown();

                _isServerRunning = true;
                Debug.DebugUtility.DebugLog("🎉 === Complete Game Server Started Successfully ===");
                Debug.DebugUtility.DebugLog($"🔗 Players can connect via TCP: {_config.TCPPort}, UDP: {_config.UDPPort}");
                Debug.DebugUtility.DebugLog($"👥 Max Players: {_config.MaxPlayers}");
                Debug.DebugUtility.DebugLog($"💾 Database: {_config.DatabaseType} at {_config.DataDirectory}");
                Debug.DebugUtility.DebugLog($"⚙️ Admin Console: Available");

                // Keep server running
                await WaitForShutdown();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to start complete server: {ex.Message}");
                throw;
            }
        }

        private void InitializeDatabase()
        {
            Debug.DebugUtility.DebugLog("💾 Initializing database...");
            
            var dbType = _config.DatabaseType.ToLower() switch
            {
                "mongodb" => DatabaseFactory.DatabaseType.MongoDB,
                "encryptedbinary" => DatabaseFactory.DatabaseType.EncryptedBinary,
                "optimizedencryptedbinary" => DatabaseFactory.DatabaseType.OptimizedEncryptedBinary,
                _ => DatabaseFactory.DatabaseType.OptimizedEncryptedBinary
            };
            
            _database = DatabaseFactory.CreateDatabase(
                dbType,
                _config.DataDirectory,
                _config.EncryptionKey
            );

            Debug.DebugUtility.DebugLog("✅ Database initialized successfully");
        }

        private void InitializeEventScheduler()
        {
            Debug.DebugUtility.DebugLog("⏰ Initializing EventScheduler...");
            
            _eventScheduler = EventScheduler.Instance;
            _schedulerExample = new EventSchedulerExample();
            _schedulerExample.InitializeServerEvents();

            // Schedule server heartbeat and monitoring
            _eventScheduler.ScheduleRecurringEvent(
                "ServerHeartbeat",
                SendServerHeartbeat,
                RecurrenceType.Seconds,
                30,
                EventPriority.Low
            );

            // Schedule player count monitoring
            _eventScheduler.ScheduleRecurringEvent(
                "PlayerCountMonitor",
                MonitorPlayerCount,
                RecurrenceType.Seconds,
                10,
                EventPriority.Normal
            );

            // Schedule database backup
            _eventScheduler.ScheduleRecurringEvent(
                "DatabaseBackup",
                BackupDatabase,
                RecurrenceType.Minutes,
                30,
                EventPriority.High
            );

            Debug.DebugUtility.DebugLog("✅ EventScheduler initialized successfully");
        }

        private void SetupGracefulShutdown()
        {
            Debug.DebugUtility.DebugLog("🛡️ Setting up graceful shutdown handlers...");
            
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private async Task WaitForShutdown()
        {
            Debug.DebugUtility.DebugLog("🎮 Server is running. Press Ctrl+C to shutdown gracefully.");
            
            try
            {
                await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.DebugUtility.DebugLog("🔄 Shutdown requested...");
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Debug.DebugUtility.DebugLog("🔄 Ctrl+C detected. Initiating graceful shutdown...");
            InitiateGracefulShutdown();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Debug.DebugUtility.DebugLog("🔄 Process exit event detected. Initiating graceful shutdown...");
            InitiateGracefulShutdown();
        }

        private void SendServerHeartbeat()
        {
            int playerCount = Server.GetCurrentPlayerCount();
            Debug.DebugUtility.DebugLog($"💓 Server heartbeat - Players: {playerCount}/{_config.MaxPlayers}");
        }

        private void MonitorPlayerCount()
        {
            int playerCount = Server.GetCurrentPlayerCount();
            if (playerCount > _config.MaxPlayers * 0.8)
            {
                Debug.DebugUtility.WarningLog($"⚠️ High player load: {playerCount}/{_config.MaxPlayers} ({(playerCount / (float)_config.MaxPlayers) * 100:F1}%)");
            }
        }

        private void BackupDatabase()
        {
            try
            {
                Debug.DebugUtility.DebugLog("💾 Creating database backup...");
                DatabaseFactory.SaveDatabaseOnShutdown(_database);
                Debug.DebugUtility.DebugLog("✅ Database backup completed");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"❌ Database backup failed: {ex.Message}");
            }
        }

        private void InitiateGracefulShutdown()
        {
            if (_cancellationTokenSource.IsCancellationRequested || !_isServerRunning)
                return;

            Task.Run(async () =>
            {
                try
                {
                    Debug.DebugUtility.DebugLog("🔄 === Starting Graceful Shutdown ===");

                    _isServerRunning = false;
                    _cancellationTokenSource.Cancel();

                    // 1. Stop accepting new connections
                    Debug.DebugUtility.DebugLog("🔒 Stopping TCP/UDP server...");
                    Server.ShutDown();

                    // 2. Save all server data
                    Debug.DebugUtility.DebugLog("💾 Saving all server data...");
                    DatabaseFactory.SaveDatabaseOnShutdown(_database);

                    // 3. Stop EventScheduler
                    Debug.DebugUtility.DebugLog("⏰ Stopping EventScheduler...");
                    _schedulerExample.Shutdown();

                    // 4. Cleanup database connection
                    if (_database is IDisposable disposableDb)
                    {
                        disposableDb.Dispose();
                    }

                    Debug.DebugUtility.DebugLog("✅ === Graceful Shutdown Complete ===");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"❌ Error during graceful shutdown: {ex.Message}");
                    Environment.Exit(1);
                }
            });
        }

        private int GetPlayerCount()
        {
            return Server.GetCurrentPlayerCount();
        }

        public void Shutdown()
        {
            Debug.DebugUtility.DebugLog("🔄 Shutdown requested from external source...");
            InitiateGracefulShutdown();
        }

        public bool IsRunning()
        {
            return _isServerRunning && Server.IsServerRunning();
        }

        public static async Task Main(string[] args)
        {
            try
            {
                var server = new ServerStartupExample();
                await server.StartServer();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Fatal server error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
} 