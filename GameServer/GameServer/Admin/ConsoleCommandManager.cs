using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using GameSystem.Lobby;
using GameSystem.VoIP;

namespace Admin
{
    public class ConsoleCommandManager
    {
        private static ConsoleCommandManager _instance;
        public static ConsoleCommandManager Instance => _instance ??= new ConsoleCommandManager();

        private readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
        private readonly List<BannedPlayer> _bannedPlayers = new List<BannedPlayer>();
        private bool _isRunning = false;
        private Thread _consoleThread;

        // Events for logging and notifications
        public event Action<string> OnCommandExecuted;
        public event Action<string, string> OnPlayerKicked;  // playerId, reason
        public event Action<string, DateTime?, string> OnPlayerBanned;  // playerId, until, reason

        private ConsoleCommandManager()
        {
            RegisterDefaultCommands();
        }

        #region Console Management

        public void StartConsole()
        {
            if (_isRunning) return;

            _isRunning = true;
            _consoleThread = new Thread(ConsoleLoop)
            {
                IsBackground = true,
                Name = "ServerConsole"
            };
            _consoleThread.Start();

            Console.WriteLine("🖥️  Server Console Started");
            Console.WriteLine("📋 Type 'help' for available commands");
            Console.WriteLine("⚡ Server is ready for administrative commands!");
            Console.WriteLine();
        }

        public void StopConsole()
        {
            _isRunning = false;
            Console.WriteLine("🖥️  Server Console Stopped");
        }

        private void ConsoleLoop()
        {
            while (_isRunning)
            {
                try
                {
                    Console.Write("Server> ");
                    string input = Console.ReadLine();
                    
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        ProcessCommand(input.Trim());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Console error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Command Processing

        public void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    var result = command.Execute(args);
                    Console.WriteLine(result.Message);
                    
                    if (result.Success)
                    {
                        OnCommandExecuted?.Invoke($"{commandName} {string.Join(" ", args)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Command execution failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Unknown command: {commandName}. Type 'help' for available commands.");
            }
        }

        public void RegisterCommand(string name, IConsoleCommand command)
        {
            _commands[name.ToLower()] = command;
        }

        private void RegisterDefaultCommands()
        {
            // Help and information commands
            RegisterCommand("help", new HelpCommand());
            RegisterCommand("info", new ServerInfoCommand());
            RegisterCommand("status", new ServerStatusCommand());

            // Player management commands
            RegisterCommand("players", new ListPlayersCommand());
            RegisterCommand("player", new PlayerInfoCommand());
            RegisterCommand("kick", new KickPlayerCommand());
            RegisterCommand("ban", new BanPlayerCommand());
            RegisterCommand("unban", new UnbanPlayerCommand());
            RegisterCommand("banned", new ListBannedCommand());

            // Room management commands
            RegisterCommand("rooms", new ListRoomsCommand());
            RegisterCommand("room", new RoomInfoCommand());
            RegisterCommand("closeroom", new CloseRoomCommand());
            RegisterCommand("broadcast", new BroadcastCommand());

            // VoIP management commands
            RegisterCommand("mute", new MutePlayerCommand());
            RegisterCommand("unmute", new UnmutePlayerCommand());
            RegisterCommand("voip", new VoIPStatusCommand());
            RegisterCommand("voice", new VoiceChannelsCommand());

            // Server control commands
            RegisterCommand("stop", new StopServerCommand());
            RegisterCommand("restart", new RestartServerCommand());
            RegisterCommand("clear", new ClearConsoleCommand());
        }

        #endregion

        #region Ban Management

        public bool IsPlayerBanned(string playerId, out BannedPlayer bannedInfo)
        {
            bannedInfo = _bannedPlayers.FirstOrDefault(b => 
                b.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase) && 
                (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow));
            
            return bannedInfo != null;
        }

        public void BanPlayer(string playerId, string playerName, TimeSpan? duration, string reason, string adminName)
        {
            // Remove existing ban if any
            _bannedPlayers.RemoveAll(b => b.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase));

            DateTime? bannedUntil = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null;
            
            var ban = new BannedPlayer
            {
                PlayerId = playerId,
                PlayerName = playerName,
                BannedAt = DateTime.UtcNow,
                BannedUntil = bannedUntil,
                Reason = reason,
                AdminName = adminName
            };

            _bannedPlayers.Add(ban);
            OnPlayerBanned?.Invoke(playerId, bannedUntil, reason);

            // Kick player if currently connected
            var player = LobbyManager.Instance.GetPlayer(playerId);
            if (player != null)
            {
                KickPlayer(playerId, $"Banned: {reason}");
            }
        }

        public void UnbanPlayer(string playerId)
        {
            int removed = _bannedPlayers.RemoveAll(b => b.PlayerId.Equals(playerId, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine(removed > 0 ? $"✅ Player {playerId} unbanned" : $"❌ Player {playerId} was not banned");
        }

        public void KickPlayer(string playerId, string reason)
        {
            var player = LobbyManager.Instance.GetPlayer(playerId);
            if (player != null)
            {
                // Remove from room if in one
                if (!string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    LobbyManager.Instance.LeaveRoom(playerId, player.CurrentRoomId);
                }

                // Disconnect the player
                player.NetClient?.Disconnect();
                
                // Remove from lobby manager
                LobbyManager.Instance.RemovePlayer(playerId);
                
                OnPlayerKicked?.Invoke(playerId, reason);
                Console.WriteLine($"✅ Player {player.Name} kicked: {reason}");
            }
            else
            {
                Console.WriteLine($"❌ Player {playerId} not found");
            }
        }

        #endregion

        #region Utility Methods

        public List<BannedPlayer> GetBannedPlayers()
        {
            return _bannedPlayers.Where(b => b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow).ToList();
        }

        public void CleanupExpiredBans()
        {
            int removed = _bannedPlayers.RemoveAll(b => b.BannedUntil.HasValue && b.BannedUntil <= DateTime.UtcNow);
            if (removed > 0)
            {
                Console.WriteLine($"🧹 Cleaned up {removed} expired bans");
            }
        }

        #endregion
    }

    #region Command Interface and Result

    public interface IConsoleCommand
    {
        CommandResult Execute(string[] args);
        string GetHelp();
    }

    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static CommandResult SuccessResult(string message) => new CommandResult { Success = true, Message = message };
        public static CommandResult ErrorResult(string message) => new CommandResult { Success = false, Message = $"❌ {message}" };
    }

    #endregion

    #region Data Structures

    public class BannedPlayer
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public DateTime BannedAt { get; set; }
        public DateTime? BannedUntil { get; set; }
        public string Reason { get; set; }
        public string AdminName { get; set; }

        public bool IsExpired => BannedUntil.HasValue && BannedUntil <= DateTime.UtcNow;
        public string DurationText => BannedUntil.HasValue ? $"until {BannedUntil:yyyy-MM-dd HH:mm:ss}" : "permanent";
    }

    #endregion
} 