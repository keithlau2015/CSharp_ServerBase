using System;
using System.Threading.Tasks;
using Utility;
using Database;
using GameServer.Examples;
using GameServer.Utility;
using Network;  // Add this for ServerConfig

namespace GameServer
{
    /// <summary>
    /// Main server launcher with command-line argument support for Unity integration
    /// </summary>
    public class ServerLauncher
    {
        private GameServerManager _server;
        
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
            
            // Sync port configuration
            config.SyncPorts();
            
            Console.WriteLine("🎮 GameServer Starting...");
            Console.WriteLine($"TCP Port: {config.TCPPort}");
            Console.WriteLine($"UDP Port: {config.UDPPort}");
            Console.WriteLine($"Max Players: {config.MaxPlayers}");
            Console.WriteLine($"Database: {config.DatabaseType}");
            Console.WriteLine($"Data Directory: {config.DataDirectory}");
            Console.WriteLine();

            // Run network diagnostics
            Console.WriteLine("🔍 Running network diagnostics...");
            var diagnostics = NetworkHelper.RunNetworkDiagnostics(config.TCPPort);
            Console.WriteLine(diagnostics.ToString());

            // Handle port conflicts
            if (!diagnostics.IsPortAvailable)
            {
                if (diagnostics.SuggestedPort > 0)
                {
                    Console.WriteLine($"⚠️ Port {config.TCPPort} is in use. Switching to port {diagnostics.SuggestedPort}");
                    config.TCPPort = diagnostics.SuggestedPort;
                    config.UDPPort = diagnostics.SuggestedPort + 1;
                    config.Port = diagnostics.SuggestedPort; // Keep in sync
                }
                else
                {
                    Console.WriteLine($"❌ No available ports found. Please specify a different port.");
                    return;
                }
            }

            // Configure firewall if needed
            if (config.ConfigureFirewall)
            {
                await ConfigureNetworkAccess(config);
            }

            // Show network information
            ShowNetworkInformation(config, diagnostics);
            
            // Initialize server with configuration
                            _server = new GameServerManager();
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

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--port":
                    case "-p":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int port))
                        {
                            config.Port = port;
                            config.TCPPort = port;
                            config.UDPPort = port + 1;
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
                        
                    case "--configure-firewall":
                    case "--firewall":
                        config.ConfigureFirewall = true;
                        break;
                        
                    case "--no-firewall":
                        config.ConfigureFirewall = false;
                        break;
                        
                    case "--show-port-forwarding":
                    case "--port-forwarding":
                        config.ShowPortForwarding = true;
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
            Console.WriteLine("  --configure-firewall       Automatically configure firewall rules");
            Console.WriteLine("  --no-firewall              Skip firewall configuration");
            Console.WriteLine("  --show-port-forwarding     Display port forwarding instructions");
            Console.WriteLine("  --help, -h                 Show this help message");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  GameServer.exe --port 8080 --maxplayers 500 --database EncryptedBinary");
            Console.WriteLine("  GameServer.exe --port 9000 --configure-firewall --show-port-forwarding");
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

        private async Task ConfigureNetworkAccess(ServerConfig config)
        {
            Console.WriteLine("🔥 Configuring network access...");
            
            if (!NetworkHelper.IsRunningAsAdmin())
            {
                Console.WriteLine("⚠️ Administrator privileges required for firewall configuration.");
                Console.WriteLine("Attempting to request elevated privileges...");
                
                string currentExecutable = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!NetworkHelper.EnsureAdminPrivileges(currentExecutable))
                {
                    Console.WriteLine("❌ Could not obtain administrator privileges.");
                    Console.WriteLine("Please run as administrator or use --no-firewall option.");
                    return;
                }
            }

            try
            {
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                bool success = await NetworkHelper.ConfigureWindowsFirewall(config.Port, appPath, "GameServer");
                
                if (success)
                {
                    Console.WriteLine("✅ Firewall configured successfully!");
                }
                else
                {
                    Console.WriteLine("⚠️ Firewall configuration may have failed. Check Windows Firewall manually.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Firewall configuration error: {ex.Message}");
            }
        }

        private void ShowNetworkInformation(ServerConfig config, NetworkDiagnostics diagnostics)
        {
            Console.WriteLine();
            Console.WriteLine("🌐 NETWORK INFORMATION");
            Console.WriteLine("══════════════════════");
            Console.WriteLine($"Local Server Address: {diagnostics.LocalIP}:{config.Port}");
            Console.WriteLine($"Localhost Address: 127.0.0.1:{config.Port}");
            Console.WriteLine();

            if (config.ShowPortForwarding)
            {
                Console.WriteLine(NetworkHelper.GeneratePortForwardingInstructions(config.Port));
            }
            else
            {
                Console.WriteLine("💡 Use --show-port-forwarding to see router configuration instructions");
            }
            
            Console.WriteLine();
        }
    }
} 