using System;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Database;
using GameServer.Examples;

namespace GameServer
{
    /// <summary>
    /// Example of how to integrate EventScheduler into your main server startup
    /// </summary>
    public class ServerStartupExample
    {
        private EventScheduler _eventScheduler;
        private EventSchedulerExample _schedulerExample;
        private DatabaseBase _database;
        private CancellationTokenSource _cancellationTokenSource;

        public async Task StartServer()
        {
            try
            {
                Debug.DebugUtility.DebugLog("=== Starting Game Server ===");
                
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize Database
                InitializeDatabase();

                // Initialize EventScheduler
                InitializeEventScheduler();

                // Setup graceful shutdown
                SetupGracefulShutdown();

                Debug.DebugUtility.DebugLog("=== Game Server Started Successfully ===");

                // Keep server running
                await WaitForShutdown();
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to start server: {ex.Message}");
                throw;
            }
        }

        private void InitializeDatabase()
        {
            Debug.DebugUtility.DebugLog("Initializing database...");
            
            _database = DatabaseFactory.CreateDatabase(
                DatabaseFactory.DatabaseType.OptimizedEncryptedBinary,
                "./GameData",
                "YourSecureEncryptionKey123!"
            );

            Debug.DebugUtility.DebugLog("Database initialized successfully");
        }

        private void InitializeEventScheduler()
        {
            Debug.DebugUtility.DebugLog("Initializing EventScheduler...");
            
            _eventScheduler = EventScheduler.Instance;
            _schedulerExample = new EventSchedulerExample();
            _schedulerExample.InitializeServerEvents();

            // Schedule server heartbeat every 30 seconds
            _eventScheduler.ScheduleRecurringEvent(
                "ServerHeartbeat",
                SendServerHeartbeat,
                RecurrenceType.Seconds,
                30,
                EventPriority.Low
            );

            Debug.DebugUtility.DebugLog("EventScheduler initialized successfully");
        }

        private void SetupGracefulShutdown()
        {
            Debug.DebugUtility.DebugLog("Setting up graceful shutdown handlers...");
            
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private async Task WaitForShutdown()
        {
            Debug.DebugUtility.DebugLog("Server is running. Press Ctrl+C to shutdown gracefully.");
            
            try
            {
                await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.DebugUtility.DebugLog("Shutdown requested...");
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Debug.DebugUtility.DebugLog("Ctrl+C detected. Initiating graceful shutdown...");
            InitiateGracefulShutdown();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Debug.DebugUtility.DebugLog("Process exit event detected. Initiating graceful shutdown...");
            InitiateGracefulShutdown();
        }

        private void SendServerHeartbeat()
        {
            Debug.DebugUtility.DebugLog($"Server heartbeat - Players: {GetPlayerCount()}");
        }

        private void InitiateGracefulShutdown()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            Task.Run(async () =>
            {
                try
                {
                    Debug.DebugUtility.DebugLog("=== Starting Graceful Shutdown ===");

                    _cancellationTokenSource.Cancel();

                    Debug.DebugUtility.DebugLog("Saving all server data...");
                    DatabaseFactory.SaveDatabaseOnShutdown(_database);

                    Debug.DebugUtility.DebugLog("Stopping EventScheduler...");
                    _schedulerExample.Shutdown();

                    if (_database is IDisposable disposableDb)
                    {
                        disposableDb.Dispose();
                    }

                    Debug.DebugUtility.DebugLog("=== Graceful Shutdown Complete ===");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Debug.DebugUtility.ErrorLog($"Error during graceful shutdown: {ex.Message}");
                    Environment.Exit(1);
                }
            });
        }

        private int GetPlayerCount()
        {
            return new Random().Next(10, 100);
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