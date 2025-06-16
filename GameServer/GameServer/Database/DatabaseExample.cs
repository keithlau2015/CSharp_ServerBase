using System;
using System.Threading;
using System.Threading.Tasks;

namespace Database
{
    // Example usage class - integrate this into your main server class
    public class DatabaseExample
    {
        private DatabaseBase _database;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public DatabaseExample()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeDatabase();
            SetupGracefulShutdown();
        }

        private void InitializeDatabase()
        {
            // Choose your database type
            var dbType = DatabaseFactory.DatabaseType.EncryptedBinary;
            
            // For encrypted binary database
            string dataDirectory = "./GameData"; // Directory to store encrypted files
            string encryptionKey = "YourSecureEncryptionKey123!"; // Use a strong key in production
            
            _database = DatabaseFactory.CreateDatabase(dbType, dataDirectory, encryptionKey);
        }

        private void SetupGracefulShutdown()
        {
            // Handle Ctrl+C and other termination signals
            Console.CancelKeyPress += OnShutdown;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnShutdown(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // Prevent immediate termination
            Debug.DebugUtility.DebugLog("Graceful shutdown initiated...");
            Shutdown();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Debug.DebugUtility.DebugLog("Process exit event triggered...");
            Shutdown();
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            
            Debug.DebugUtility.DebugLog("Saving all database data...");
            DatabaseFactory.SaveDatabaseOnShutdown(_database);
            
            if (_database is IDisposable disposableDb)
            {
                disposableDb.Dispose();
            }
            
            Debug.DebugUtility.DebugLog("Database shutdown complete.");
        }

        // Example methods showing how to use the database
        public async Task ExampleUsage()
        {
            // Example data class
            var playerData = new PlayerData 
            { 
                UID = "player123", 
                Name = "TestPlayer", 
                Level = 10,
                LastLogin = DateTime.Now
            };

            try
            {
                // Create/Save player data
                await _database.CRUD_Instance(DatabaseBase.Action.Create, "GameDB", playerData);

                // Read player data
                var readPlayer = new PlayerData { UID = "player123" };
                await _database.CRUD_Instance(DatabaseBase.Action.Read, "GameDB", readPlayer);
                Debug.DebugUtility.DebugLog($"Read player: {readPlayer.Name}, Level: {readPlayer.Level}");

                // Update player data
                readPlayer.Level = 15;
                await _database.CRUD_Instance(DatabaseBase.Action.Update, "GameDB", readPlayer);

                // If using EncryptedBinaryDBManager, you can also use additional methods
                if (_database is EncryptedBinaryDBManager binaryDb)
                {
                    var allPlayers = await binaryDb.GetAllAsync<PlayerData>("GameDB");
                    Debug.DebugUtility.DebugLog($"Total players: {allPlayers.Count}");

                    var specificPlayer = await binaryDb.GetByIdAsync<PlayerData>("GameDB", "UID", "player123");
                    if (specificPlayer != null)
                    {
                        Debug.DebugUtility.DebugLog($"Found player: {specificPlayer.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Database operation failed: {ex.Message}");
            }
        }

        // Example data class
        public class PlayerData
        {
            public string UID { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
            public DateTime LastLogin { get; set; }
        }

        // Configuration data example
        public class ServerConfig
        {
            public string ID { get; set; }
            public string ServerName { get; set; }
            public int MaxPlayers { get; set; }
            public bool MaintenanceMode { get; set; }
        }

        public async Task ExampleConfigUsage()
        {
            var config = new ServerConfig
            {
                ID = "server_config",
                ServerName = "GameServer",
                MaxPlayers = 1000,
                MaintenanceMode = false
            };

            // Save configuration
            await _database.CRUD_Config(DatabaseBase.Action.Create, "ConfigDB", config);

            // Read configuration
            var readConfig = new ServerConfig { ID = "server_config" };
            await _database.CRUD_Config(DatabaseBase.Action.Read, "ConfigDB", readConfig);
            
            Debug.DebugUtility.DebugLog($"Server: {readConfig.ServerName}, Max Players: {readConfig.MaxPlayers}");
        }
    }
} 