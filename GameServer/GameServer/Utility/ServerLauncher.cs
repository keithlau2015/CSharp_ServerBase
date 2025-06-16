using System;
using System.Threading.Tasks;
using Utility;
using Database;
using GameServer.Examples;

namespace GameServer
{
    /// <summary>
    /// Main server launcher with command-line argument support for Unity integration
    /// </summary>
    public class ServerLauncher
    {
        private ServerStartupExample _server;
        
        public static async Task Main(string[] args)
        {
            try
            {
                var launcher = new ServerLauncher();
                await launcher.StartWithArgs(args);
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Fatal server error: {ex.Message}");
                Console.WriteLine($"Server failed to start: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        public async Task StartWithArgs(string[] args)
        {
            // Parse command line arguments
            var config = ParseArguments(args);
            
            Console.WriteLine("🎮 GameServer Starting...");
            Console.WriteLine($"Port: {config.Port}");
            Console.WriteLine($"Max Players: {config.MaxPlayers}");
            Console.WriteLine($"Database: {config.DatabaseType}");
            Console.WriteLine($"Data Directory: {config.DataDirectory}");
            
            // Initialize server with configuration
            _server = new ServerStartupExample();
            await _server.StartServerWithConfig(config);
            
            // Keep server running
            Console.WriteLine("✅ Server started successfully!");
            Console.WriteLine("Press 'q' to quit or Ctrl+C for graceful shutdown");
            
            // Handle console input for shutdown
            await HandleConsoleInput();
        }

        private ServerConfig ParseArguments(string[] args)
        {
            var config = new ServerConfig
            {
                Port = 8080,
                MaxPlayers = 100,
                DatabaseType = "EncryptedBinary",
                DataDirectory = "./GameData",
                EncryptionKey = "DefaultGameServerKey2024!",
                AutoStart = true
            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--port":
                    case "-p":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                        {
                            config.Port = port;
                            i++;
                        }
                        break;
                        
                    case "--maxplayers":
                    case "-m":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int maxPlayers))
                        {
                            config.MaxPlayers = maxPlayers;
                            i++;
                        }
                        break;
                        
                    case "--database":
                    case "-db":
                        if (i + 1 < args.Length)
                        {
                            config.DatabaseType = args[i + 1];
                            i++;
                        }
                        break;
                        
                    case "--datadir":
                    case "-d":
                        if (i + 1 < args.Length)
                        {
                            config.DataDirectory = args[i + 1];
                            i++;
                        }
                        break;
                        
                    case "--key":
                    case "-k":
                        if (i + 1 < args.Length)
                        {
                            config.EncryptionKey = args[i + 1];
                            i++;
                        }
                        break;
                        
                    case "--noautostart":
                        config.AutoStart = false;
                        break;
                        
                    case "--help":
                    case "-h":
                        ShowHelp();
                        Environment.Exit(0);
                        break;
                }
            }

            return config;
        }

        private void ShowHelp()
        {
            Console.WriteLine("🎮 GameServer Command Line Arguments:");
            Console.WriteLine("");
            Console.WriteLine("  --port, -p <number>        Server port (default: 8080)");
            Console.WriteLine("  --maxplayers, -m <number>  Maximum players (default: 100)");
            Console.WriteLine("  --database, -db <type>     Database type: EncryptedBinary|MongoDB (default: EncryptedBinary)");
            Console.WriteLine("  --datadir, -d <path>       Data directory path (default: ./GameData)");
            Console.WriteLine("  --key, -k <key>            Encryption key for binary database");
            Console.WriteLine("  --noautostart              Don't start scheduler automatically");
            Console.WriteLine("  --help, -h                 Show this help message");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("  GameServer.exe --port 8080 --maxplayers 500 --database EncryptedBinary");
        }

        private async Task HandleConsoleInput()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        Console.WriteLine("Shutting down server...");
                        _server?.Shutdown();
                        break;
                    }
                }
            });
        }
    }

    public class ServerConfig
    {
        public int Port { get; set; }
        public int MaxPlayers { get; set; }
        public string DatabaseType { get; set; }
        public string DataDirectory { get; set; }
        public string EncryptionKey { get; set; }
        public bool AutoStart { get; set; }
    }
} 