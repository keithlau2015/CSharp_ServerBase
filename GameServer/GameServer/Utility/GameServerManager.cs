using System;
using System.Threading.Tasks;
using Database;
using Utility;
using GameServer.Examples;
using System.Threading;
using Network;
using System.Diagnostics;

namespace GameServer.Utility
{
    /// <summary>
    /// Production-ready game server manager that orchestrates all server components:
    /// Database, EventScheduler, TCP/UDP Networking, and Admin Console
    /// </summary>
    public class GameServerManager
    {
        private EventScheduler _eventScheduler;
        private EventSchedulerExample _schedulerComponent;
        private DatabaseBase _database;
        private CancellationTokenSource _cancellationTokenSource;
        private ServerConfig _config;
        private bool _isServerRunning = false;
        private bool _isShuttingDown = false;

        #region Public Properties
        
        /// <summary>
        /// Gets whether the server is currently running
        /// </summary>
        public bool IsRunning => _isServerRunning && Server.IsServerRunning() && !_isShuttingDown;
        
        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        public ServerConfig Configuration => _config;
        
        /// <summary>
        /// Gets the current number of connected players
        /// </summary>
        public int CurrentPlayerCount => Server.GetCurrentPlayerCount();
        
        /// <summary>
        /// Gets the database instance
        /// </summary>
        public DatabaseBase Database => _database;

        #endregion

        #region Events
        
        /// <summary>
        /// Event fired when the server starts successfully
        /// </summary>
        public event Action OnServerStarted;
        
        /// <summary>
        /// Event fired when the server stops
        /// </summary>
        public event Action OnServerStopped;
        
        /// <summary>
        /// Event fired when a server error occurs
        /// </summary>
        public event Action<string> OnServerError;

        #endregion

        #region Server Lifecycle

        /// <summary>
        /// Starts the server with default configuration
        /// </summary>
        public async Task StartServer()
        {
            var defaultConfig = CreateDefaultConfig();
            await StartServerWithConfig(defaultConfig);
        }

        /// <summary>
        /// Starts the server with the specified configuration
        /// </summary>
        /// <param name="config">Server configuration</param>
        public async Task StartServerWithConfig(ServerConfig config)
        {
            if (_isServerRunning)
            {
                Debug.DebugUtility.WarningLog("Server is already running!");
                return;
            }

            try
            {
                _config = config;
                _isShuttingDown = false;
                
                Debug.DebugUtility.DebugLog("🚀 === Initializing Game Server ===");
                Debug.DebugUtility.DebugLog($"Server Name: {config.Name}");
                Debug.DebugUtility.DebugLog($"TCP Port: {config.TCPPort}, UDP Port: {config.UDPPort}");
                Debug.DebugUtility.DebugLog($"Max Players: {config.MaxPlayers}");
                
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize all components in order
                await InitializeDatabase();
                await InitializeEventScheduler();
                await StartNetworkServer();
                
                SetupGracefulShutdown();
                StartServerMonitoring();

                _isServerRunning = true;
                
                Debug.DebugUtility.DebugLog("🎉 === Game Server Started Successfully ===");
                Debug.DebugUtility.DebugLog($"🔗 Ready for connections on TCP:{_config.TCPPort}, UDP:{_config.UDPPort}");
                
                OnServerStarted?.Invoke();

                // Keep server running
                await WaitForShutdown();
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to start server: {ex.Message}";
                Debug.DebugUtility.ErrorLog(errorMsg);
                OnServerError?.Invoke(errorMsg);
                throw;
            }
        }

        /// <summary>
        /// Stops the server gracefully
        /// </summary>
        public async Task StopServer()
        {
            if (!_isServerRunning || _isShuttingDown)
                return;

            Debug.DebugUtility.DebugLog("🔄 Initiating server shutdown...");
            await InitiateGracefulShutdown();
        }

        #endregion

        #region Initialization Methods

        private ServerConfig CreateDefaultConfig()
        {
            return new ServerConfig
            {
                ID = 1,
                Name = "GameServer",
                Port = 8080,
                TCPPort = 8080,
                UDPPort = 8081,
                MaxPlayers = 100,
                DatabaseType = "OptimizedEncryptedBinary",
                DataDirectory = "./GameData",
                EncryptionKey = "DefaultGameServerKey2024!",
                AutoStart = true,
                DebugLevel = 1
            };
        }

        private async Task InitializeDatabase()
        {
            Debug.DebugUtility.DebugLog("💾 Initializing database system...");
            
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

            Debug.DebugUtility.DebugLog($"✅ Database initialized: {_config.DatabaseType}");
            
            // Small delay to ensure database is ready
            await Task.Delay(100);
        }

        private async Task InitializeEventScheduler()
        {
            Debug.DebugUtility.DebugLog("⏰ Initializing event scheduler...");
            
            _eventScheduler = EventScheduler.Instance;
            _schedulerComponent = new EventSchedulerExample();
            _schedulerComponent.InitializeServerEvents();

            // Schedule essential server monitoring events
            ScheduleServerEvents();

            Debug.DebugUtility.DebugLog("✅ Event scheduler initialized");
            
            // Small delay to ensure scheduler is ready
            await Task.Delay(100);
        }

        private async Task StartNetworkServer()
        {
            Debug.DebugUtility.DebugLog("🌐 Starting TCP/UDP network server...");
            
            await Server.StartServerWithConfig(_config);
            
            Debug.DebugUtility.DebugLog("✅ Network server started");
            
            // Small delay to ensure network is ready
            await Task.Delay(100);
        }

        #endregion

        #region Event Scheduling & Monitoring

        private void ScheduleServerEvents()
        {
            // Server heartbeat - every 30 seconds
            _eventScheduler.ScheduleRecurringEvent(
                "ServerHeartbeat",
                () => ServerHeartbeat(),
                RecurrenceType.Seconds,
                30,
                EventPriority.Low
            );

            // Database backup - every 30 minutes
            _eventScheduler.ScheduleRecurringEvent(
                "DatabaseBackup",
                () => PerformDatabaseBackup(),
                RecurrenceType.Minutes,
                30,
                EventPriority.High
            );
        }

        private void ServerHeartbeat()
        {
            if (!_isServerRunning) return;
            
            int playerCount = Server.GetCurrentPlayerCount();
            Debug.DebugUtility.DebugLog($"💓 Server Status - Players: {playerCount}/{_config.MaxPlayers}");
        }

        private void PerformDatabaseBackup()
        {
            if (!_isServerRunning) return;
            
            try
            {
                Debug.DebugUtility.DebugLog("💾 Performing database backup...");
                DatabaseFactory.SaveDatabaseOnShutdown(_database);
                Debug.DebugUtility.DebugLog("✅ Database backup completed");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"❌ Database backup failed: {ex.Message}");
                OnServerError?.Invoke($"Database backup failed: {ex.Message}");
            }
        }

        private void StartServerMonitoring()
        {
            Debug.DebugUtility.DebugLog("📊 Starting server monitoring...");
            
            Task.Run(async () =>
            {
                while (_isServerRunning && !_isShuttingDown)
                {
                    try
                    {
                        await Task.Delay(60000, _cancellationTokenSource.Token);
                        
                        if (!Server.IsServerRunning())
                        {
                            Debug.DebugUtility.ErrorLog("🚨 Network server stopped unexpectedly!");
                            OnServerError?.Invoke("Network server stopped unexpectedly");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.DebugUtility.ErrorLog($"Server monitoring error: {ex.Message}");
                    }
                }
            });
        }

        #endregion

        #region Shutdown Handling

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
                Debug.DebugUtility.DebugLog("🔄 Shutdown signal received...");
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Debug.DebugUtility.DebugLog("🔄 Ctrl+C detected. Initiating graceful shutdown...");
            Task.Run(async () => await InitiateGracefulShutdown());
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Debug.DebugUtility.DebugLog("🔄 Process exit event detected. Initiating graceful shutdown...");
            Task.Run(async () => await InitiateGracefulShutdown());
        }

        private async Task InitiateGracefulShutdown()
        {
            if (_isShuttingDown || !_isServerRunning)
                return;

            _isShuttingDown = true;

            try
            {
                Debug.DebugUtility.DebugLog("🔄 === Starting Graceful Shutdown ===");

                _cancellationTokenSource?.Cancel();

                // Stop network server
                Debug.DebugUtility.DebugLog("🔒 Stopping network server...");
                Server.ShutDown();
                await Task.Delay(1000);

                // Save server data
                Debug.DebugUtility.DebugLog("💾 Saving server data...");
                DatabaseFactory.SaveDatabaseOnShutdown(_database);

                // Stop event scheduler
                Debug.DebugUtility.DebugLog("⏰ Stopping event scheduler...");
                _schedulerComponent?.Shutdown();

                // Cleanup database
                if (_database is IDisposable disposableDb)
                {
                    disposableDb.Dispose();
                }

                _isServerRunning = false;
                
                Debug.DebugUtility.DebugLog("✅ === Graceful Shutdown Complete ===");
                
                OnServerStopped?.Invoke();
                
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"❌ Error during shutdown: {ex.Message}");
                OnServerError?.Invoke($"Shutdown error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces an immediate database backup
        /// </summary>
        public void BackupDatabase()
        {
            PerformDatabaseBackup();
        }

        /// <summary>
        /// Gets server status information
        /// </summary>
        public string GetServerStatusString()
        {
            var upTime = DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime.ToUniversalTime());
            var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            
            return $"Running: {IsRunning} | Players: {CurrentPlayerCount}/{_config?.MaxPlayers ?? 0} | " +
                   $"Uptime: {upTime:hh\\:mm\\:ss} | Memory: {memoryMB} MB | DB: {_config?.DatabaseType ?? "Unknown"}";
        }

        /// <summary>
        /// Backward compatibility method for ServerLauncher
        /// </summary>
        public void Shutdown()
        {
            Task.Run(async () => await StopServer());
        }

        #endregion

        /// <summary>
        /// Main entry point for standalone server execution
        /// </summary>
        public static async Task Main(string[] args)
        {
            try
            {
                var serverManager = new GameServerManager();
                
                serverManager.OnServerError += (error) => 
                {
                    Console.WriteLine($"❌ Server Error: {error}");
                };
                
                await serverManager.StartServer();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"💥 Fatal server error: {ex.Message}");
                Console.WriteLine($"❌ Fatal Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
} 