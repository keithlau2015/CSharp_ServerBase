using System;
using System.Linq;
using System.Text;
using GameSystem.Lobby;
using GameSystem.VoIP;
using Network;

namespace Admin
{
    #region Help and Information Commands

    public class HelpCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var help = new StringBuilder();
            help.AppendLine("🖥️  Available Server Commands:");
            help.AppendLine();
            help.AppendLine("📊 Information Commands:");
            help.AppendLine("  help                     - Show this help");
            help.AppendLine("  info                     - Show server information");
            help.AppendLine("  status                   - Show server status");
            help.AppendLine();
            help.AppendLine("👥 Player Management:");
            help.AppendLine("  players                  - List all connected players");
            help.AppendLine("  player <id>              - Show player details");
            help.AppendLine("  kick <id> [reason]       - Kick a player");
            help.AppendLine("  ban <id> [duration] [reason] - Ban a player (duration: 1h, 1d, 7d, permanent)");
            help.AppendLine("  unban <id>               - Unban a player");
            help.AppendLine("  banned                   - List banned players");
            help.AppendLine();
            help.AppendLine("🏠 Room Management:");
            help.AppendLine("  rooms                    - List all rooms");
            help.AppendLine("  room <id>                - Show room details");
            help.AppendLine("  closeroom <id>           - Close a room");
            help.AppendLine("  broadcast <message>      - Broadcast to all players");
            help.AppendLine();
            help.AppendLine("🎤 VoIP Management:");
            help.AppendLine("  mute <id>                - Mute player's microphone");
            help.AppendLine("  unmute <id>              - Unmute player's microphone");
            help.AppendLine("  voip                     - Show VoIP status");
            help.AppendLine("  voice                    - List voice channels");
            help.AppendLine();
            help.AppendLine("🔧 Server Control:");
            help.AppendLine("  stop                     - Stop the server");
            help.AppendLine("  restart                  - Restart the server");
            help.AppendLine("  clear                    - Clear console");

            return CommandResult.SuccessResult(help.ToString());
        }

        public string GetHelp() => "Show available commands";
    }

    public class ServerInfoCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var info = new StringBuilder();
            info.AppendLine("🖥️  Server Information:");
            info.AppendLine($"   Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"   Uptime: {DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime:dd\\:hh\\:mm\\:ss}");
            info.AppendLine($"   Players Online: {LobbyManager.Instance.GetAllPlayers().Count}");
            info.AppendLine($"   Active Rooms: {LobbyManager.Instance.GetAllRooms().Count}");
            info.AppendLine($"   Memory Usage: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            
            try
            {
                var voipManager = VoIPManager.Instance;
                info.AppendLine($"   VoIP Status: ✅ Active");
            }
            catch
            {
                info.AppendLine($"   VoIP Status: ❌ Inactive");
            }

            return CommandResult.SuccessResult(info.ToString());
        }

        public string GetHelp() => "Show server information and statistics";
    }

    public class ServerStatusCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var players = LobbyManager.Instance.GetAllPlayers();
            var rooms = LobbyManager.Instance.GetAllRooms();
            var bannedCount = ConsoleCommandManager.Instance.GetBannedPlayers().Count;

            var status = new StringBuilder();
            status.AppendLine("📊 Server Status:");
            status.AppendLine($"   🟢 Online Players: {players.Count}");
            status.AppendLine($"   🏠 Active Rooms: {rooms.Count}");
            status.AppendLine($"   🚫 Banned Players: {bannedCount}");
            status.AppendLine();

            if (players.Any())
            {
                status.AppendLine("👥 Recent Player Activity:");
                foreach (var player in players.Take(5))
                {
                    var roomInfo = !string.IsNullOrEmpty(player.CurrentRoomId) ? 
                        $"in room {LobbyManager.Instance.GetRoom(player.CurrentRoomId)?.Name ?? "Unknown"}" : 
                        "in lobby";
                    status.AppendLine($"   • {player.Name} ({player.UID}) - {roomInfo}");
                }
            }

            return CommandResult.SuccessResult(status.ToString());
        }

        public string GetHelp() => "Show current server status and activity";
    }

    #endregion

    #region Player Management Commands

    public class ListPlayersCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var players = LobbyManager.Instance.GetAllPlayers();
            
            if (!players.Any())
            {
                return CommandResult.SuccessResult("👥 No players currently connected");
            }

            var result = new StringBuilder();
            result.AppendLine($"👥 Connected Players ({players.Count}):");
            
            foreach (var player in players.OrderBy(p => p.Name))
            {
                var roomInfo = "";
                if (!string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    var room = LobbyManager.Instance.GetRoom(player.CurrentRoomId);
                    roomInfo = $" (Room: {room?.Name ?? "Unknown"})";
                }

                try
                {
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    var voiceInfo = voiceState.IsMuted ? " 🔇" : voiceState.IsTalking ? " 🎤" : "";
                    result.AppendLine($"   • {player.Name} [{player.UID}]{roomInfo}{voiceInfo}");
                }
                catch
                {
                    result.AppendLine($"   • {player.Name} [{player.UID}]{roomInfo}");
                }
            }

            return CommandResult.SuccessResult(result.ToString());
        }

        public string GetHelp() => "List all connected players";
    }

    public class PlayerInfoCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: player <playerId>");

            string playerId = args[0];
            var player = LobbyManager.Instance.GetPlayer(playerId);
            
            if (player == null)
                return CommandResult.ErrorResult($"Player '{playerId}' not found");

            var info = new StringBuilder();
            info.AppendLine($"👤 Player Information:");
            info.AppendLine($"   Name: {player.Name}");
            info.AppendLine($"   ID: {player.UID}");
            info.AppendLine($"   Position: ({player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1})");
            info.AppendLine($"   Health: {player.Health}/{player.MaxHealth}");
            info.AppendLine($"   Score: {player.Score}");
            info.AppendLine($"   Kills: {player.Kills} | Deaths: {player.Deaths} | K/D: {player.KillDeathRatio:F2}");

            if (!string.IsNullOrEmpty(player.CurrentRoomId))
            {
                var room = LobbyManager.Instance.GetRoom(player.CurrentRoomId);
                info.AppendLine($"   Current Room: {room?.Name ?? "Unknown"} ({player.CurrentRoomId})");
            }
            else
            {
                info.AppendLine($"   Current Room: None (in lobby)");
            }

            // VoIP information
            try
            {
                var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                info.AppendLine($"   🎤 Voice Status:");
                info.AppendLine($"      Muted: {(voiceState.IsMuted ? "Yes" : "No")}");
                info.AppendLine($"      Deafened: {(voiceState.IsDeafened ? "Yes" : "No")}");
                info.AppendLine($"      Talking: {(voiceState.IsTalking ? "Yes" : "No")}");
                info.AppendLine($"      Volume: {voiceState.Volume:F2}");
                info.AppendLine($"      Last Activity: {voiceState.LastActivity:HH:mm:ss}");
            }
            catch
            {
                info.AppendLine($"   🎤 Voice Status: Not available");
            }

            return CommandResult.SuccessResult(info.ToString());
        }

        public string GetHelp() => "Show detailed information about a player";
    }

    public class KickPlayerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: kick <playerId> [reason]");

            string playerId = args[0];
            string reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "No reason provided";

            var player = LobbyManager.Instance.GetPlayer(playerId);
            if (player == null)
                return CommandResult.ErrorResult($"Player '{playerId}' not found");

            ConsoleCommandManager.Instance.KickPlayer(playerId, reason);
            return CommandResult.SuccessResult($"✅ Player '{player.Name}' has been kicked. Reason: {reason}");
        }

        public string GetHelp() => "Kick a player from the server";
    }

    public class BanPlayerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: ban <playerId> [duration] [reason]");

            string playerId = args[0];
            var player = LobbyManager.Instance.GetPlayer(playerId);
            if (player == null)
                return CommandResult.ErrorResult($"Player '{playerId}' not found");

            TimeSpan? duration = null;
            string reason = "No reason provided";
            
            if (args.Length > 1)
            {
                // Try to parse duration
                if (TryParseDuration(args[1], out var parsedDuration))
                {
                    duration = parsedDuration;
                    reason = args.Length > 2 ? string.Join(" ", args.Skip(2)) : reason;
                }
                else
                {
                    // No duration, all args are reason
                    reason = string.Join(" ", args.Skip(1));
                }
            }

            ConsoleCommandManager.Instance.BanPlayer(playerId, player.Name, duration, reason, "Console");
            
            string durationText = duration.HasValue ? $"for {FormatDuration(duration.Value)}" : "permanently";
            return CommandResult.SuccessResult($"✅ Player '{player.Name}' has been banned {durationText}. Reason: {reason}");
        }

        private bool TryParseDuration(string input, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            
            if (string.IsNullOrEmpty(input)) return false;
            
            input = input.ToLower();
            
            if (input == "permanent" || input == "perm")
            {
                return false; // Permanent ban (no duration)
            }

            // Parse formats like: 1h, 2d, 30m, 1w
            if (input.Length < 2) return false;
            
            string numberPart = input.Substring(0, input.Length - 1);
            string unitPart = input.Substring(input.Length - 1);
            
            if (!int.TryParse(numberPart, out int value)) return false;
            
            switch (unitPart)
            {
                case "m": duration = TimeSpan.FromMinutes(value); return true;
                case "h": duration = TimeSpan.FromHours(value); return true;
                case "d": duration = TimeSpan.FromDays(value); return true;
                case "w": duration = TimeSpan.FromDays(value * 7); return true;
                default: return false;
            }
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays} day(s)";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} hour(s)";
            return $"{(int)duration.TotalMinutes} minute(s)";
        }

        public string GetHelp() => "Ban a player (duration: 1h, 1d, 7d, permanent)";
    }

    public class UnbanPlayerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: unban <playerId>");

            string playerId = args[0];
            ConsoleCommandManager.Instance.UnbanPlayer(playerId);
            return CommandResult.SuccessResult($"✅ Player '{playerId}' has been unbanned");
        }

        public string GetHelp() => "Remove a ban from a player";
    }

    public class ListBannedCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var bannedPlayers = ConsoleCommandManager.Instance.GetBannedPlayers();
            
            if (!bannedPlayers.Any())
                return CommandResult.SuccessResult("🚫 No players are currently banned");

            var result = new StringBuilder();
            result.AppendLine($"🚫 Banned Players ({bannedPlayers.Count}):");
            
            foreach (var ban in bannedPlayers.OrderBy(b => b.BannedAt))
            {
                result.AppendLine($"   • {ban.PlayerName} [{ban.PlayerId}]");
                result.AppendLine($"     Banned: {ban.BannedAt:yyyy-MM-dd HH:mm:ss} ({ban.DurationText})");
                result.AppendLine($"     Reason: {ban.Reason}");
                result.AppendLine($"     By: {ban.AdminName}");
                result.AppendLine();
            }

            return CommandResult.SuccessResult(result.ToString());
        }

        public string GetHelp() => "List all banned players";
    }

    #endregion

    #region Room Management Commands

    public class ListRoomsCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            var rooms = LobbyManager.Instance.GetAllRooms();
            
            if (!rooms.Any())
                return CommandResult.SuccessResult("🏠 No rooms currently exist");

            var result = new StringBuilder();
            result.AppendLine($"🏠 Active Rooms ({rooms.Count}):");
            
            foreach (var room in rooms.OrderBy(r => r.Name))
            {
                var statusIcon = room.IsGameStarted ? "🎮" : room.IsFull ? "🔒" : "🟢";
                var privateIcon = room.IsPrivate ? "🔐" : "";
                
                result.AppendLine($"   {statusIcon} {room.Name} [{room.Id}] {privateIcon}");
                result.AppendLine($"      Players: {room.GetPlayerCount()}/{room.MaxPlayers} | Status: {room.State}");
                
                var players = room.GetPlayers().Take(3);
                if (players.Any())
                {
                    var playerNames = string.Join(", ", players.Select(p => p.Name));
                    if (room.GetPlayerCount() > 3)
                        playerNames += $" (+{room.GetPlayerCount() - 3} more)";
                    result.AppendLine($"      Players: {playerNames}");
                }
                result.AppendLine();
            }

            return CommandResult.SuccessResult(result.ToString());
        }

        public string GetHelp() => "List all active rooms";
    }

    public class RoomInfoCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: room <roomId>");

            string roomId = args[0];
            var room = LobbyManager.Instance.GetRoom(roomId);
            
            if (room == null)
                return CommandResult.ErrorResult($"Room '{roomId}' not found");

            var info = new StringBuilder();
            info.AppendLine($"🏠 Room Information:");
            info.AppendLine($"   Name: {room.Name}");
            info.AppendLine($"   ID: {room.Id}");
            info.AppendLine($"   Status: {room.State}");
            info.AppendLine($"   Players: {room.GetPlayerCount()}/{room.MaxPlayers}");
            info.AppendLine($"   Private: {(room.IsPrivate ? "Yes" : "No")}");
            info.AppendLine($"   Game Started: {(room.IsGameStarted ? "Yes" : "No")}");
            info.AppendLine($"   Created: {room.CreatedAt:yyyy-MM-dd HH:mm:ss}");

            var players = room.GetPlayers();
            if (players.Any())
            {
                info.AppendLine();
                info.AppendLine($"👥 Players in Room:");
                foreach (var player in players)
                {
                    try
                    {
                        var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                        var voiceIcon = voiceState.IsMuted ? " 🔇" : voiceState.IsTalking ? " 🎤" : "";
                        info.AppendLine($"   • {player.Name} [{player.UID}]{voiceIcon}");
                    }
                    catch
                    {
                        info.AppendLine($"   • {player.Name} [{player.UID}]");
                    }
                }
            }

            // Voice channel information
            try
            {
                var voiceChannel = VoIPManager.Instance.GetVoiceChannel(roomId);
                if (voiceChannel != null)
                {
                    info.AppendLine();
                    info.AppendLine($"🎤 Voice Channel:");
                    info.AppendLine($"   Active Speakers: {voiceChannel.ActiveSpeakers}");
                    info.AppendLine($"   Last Activity: {voiceChannel.LastActivity:HH:mm:ss}");
                }
            }
            catch
            {
                info.AppendLine();
                info.AppendLine($"🎤 Voice Channel: Not available");
            }

            return CommandResult.SuccessResult(info.ToString());
        }

        public string GetHelp() => "Show detailed information about a room";
    }

    public class CloseRoomCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: closeroom <roomId>");

            string roomId = args[0];
            var room = LobbyManager.Instance.GetRoom(roomId);
            
            if (room == null)
                return CommandResult.ErrorResult($"Room '{roomId}' not found");

            string roomName = room.Name;
            bool success = LobbyManager.Instance.DestroyRoom(roomId);
            
            if (success)
                return CommandResult.SuccessResult($"✅ Room '{roomName}' has been closed");
            else
                return CommandResult.ErrorResult($"Failed to close room '{roomName}'");
        }

        public string GetHelp() => "Close a room and kick all players";
    }

    public class BroadcastCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: broadcast <message>");

            string message = string.Join(" ", args);
            var players = LobbyManager.Instance.GetAllPlayers();
            
            if (!players.Any())
                return CommandResult.ErrorResult("No players connected to broadcast to");

            var broadcastMessage = new ServerBroadcast
            {
                Message = message,
                Timestamp = DateTime.UtcNow,
                Type = "ServerAnnouncement"
            };

            foreach (var player in players)
            {
                try
                {
                    var packet = new Packet("ServerBroadcast");
                    packet.Write(broadcastMessage);
                    player.NetClient?.Send(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send broadcast to {player.Name}: {ex.Message}");
                }
            }

            return CommandResult.SuccessResult($"📢 Broadcast sent to {players.Count} player(s): {message}");
        }

        public string GetHelp() => "Send a message to all connected players";
    }

    #endregion

    #region VoIP Management Commands

    public class MutePlayerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: mute <playerId>");

            string playerId = args[0];
            var player = LobbyManager.Instance.GetPlayer(playerId);
            
            if (player == null)
                return CommandResult.ErrorResult($"Player '{playerId}' not found");

            try
            {
                VoIPManager.Instance.SetPlayerMuted(playerId, true);
                return CommandResult.SuccessResult($"🔇 Player '{player.Name}' has been muted");
            }
            catch (Exception ex)
            {
                return CommandResult.ErrorResult($"Failed to mute player: {ex.Message}");
            }
        }

        public string GetHelp() => "Mute a player's microphone";
    }

    public class UnmutePlayerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            if (args.Length == 0)
                return CommandResult.ErrorResult("Usage: unmute <playerId>");

            string playerId = args[0];
            var player = LobbyManager.Instance.GetPlayer(playerId);
            
            if (player == null)
                return CommandResult.ErrorResult($"Player '{playerId}' not found");

            try
            {
                VoIPManager.Instance.SetPlayerMuted(playerId, false);
                return CommandResult.SuccessResult($"🎤 Player '{player.Name}' has been unmuted");
            }
            catch (Exception ex)
            {
                return CommandResult.ErrorResult($"Failed to unmute player: {ex.Message}");
            }
        }

        public string GetHelp() => "Unmute a player's microphone";
    }

    public class VoIPStatusCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            try
            {
                var voipManager = VoIPManager.Instance;
                var players = LobbyManager.Instance.GetAllPlayers();
                
                var result = new StringBuilder();
                result.AppendLine("🎤 VoIP System Status:");
                result.AppendLine($"   Status: ✅ Active");
                result.AppendLine($"   Players with Voice: {players.Count}");
                
                int mutedCount = 0, talkingCount = 0, deafenedCount = 0;
                
                foreach (var player in players)
                {
                    try
                    {
                        var voiceState = voipManager.GetPlayerVoiceState(player.UID);
                        if (voiceState.IsMuted) mutedCount++;
                        if (voiceState.IsTalking) talkingCount++;
                        if (voiceState.IsDeafened) deafenedCount++;
                    }
                    catch { }
                }
                
                result.AppendLine($"   Currently Talking: {talkingCount}");
                result.AppendLine($"   Muted Players: {mutedCount}");
                result.AppendLine($"   Deafened Players: {deafenedCount}");
                
                return CommandResult.SuccessResult(result.ToString());
            }
            catch (Exception ex)
            {
                return CommandResult.ErrorResult($"VoIP system not available: {ex.Message}");
            }
        }

        public string GetHelp() => "Show VoIP system status and statistics";
    }

    public class VoiceChannelsCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            try
            {
                var rooms = LobbyManager.Instance.GetAllRooms();
                var result = new StringBuilder();
                result.AppendLine("🔊 Active Voice Channels:");
                
                if (!rooms.Any())
                {
                    result.AppendLine("   No voice channels active");
                }
                else
                {
                    foreach (var room in rooms)
                    {
                        try
                        {
                            var voiceChannel = VoIPManager.Instance.GetVoiceChannel(room.Id);
                            if (voiceChannel != null)
                            {
                                result.AppendLine($"   🏠 {room.Name} [{room.Id}]");
                                result.AppendLine($"      Active Speakers: {voiceChannel.ActiveSpeakers}");
                                result.AppendLine($"      Last Activity: {voiceChannel.LastActivity:HH:mm:ss}");
                                result.AppendLine($"      Status: {(voiceChannel.IsActive ? "Active" : "Inactive")}");
                                result.AppendLine();
                            }
                        }
                        catch
                        {
                            result.AppendLine($"   🏠 {room.Name} - Voice channel not available");
                        }
                    }
                }
                
                return CommandResult.SuccessResult(result.ToString());
            }
            catch (Exception ex)
            {
                return CommandResult.ErrorResult($"Failed to get voice channels: {ex.Message}");
            }
        }

        public string GetHelp() => "List all active voice channels";
    }

    #endregion

    #region Server Control Commands

    public class StopServerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            Console.WriteLine("🛑 Server shutdown initiated...");
            
            // Notify all players about shutdown
            var players = LobbyManager.Instance.GetAllPlayers();
            foreach (var player in players)
            {
                try
                {
                    var shutdownMessage = new ServerBroadcast
                    {
                        Message = "Server is shutting down. Please reconnect later.",
                        Type = "ServerShutdown",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    var packet = new Packet("ServerBroadcast");
                    packet.Write(shutdownMessage);
                    player.NetClient?.Send(packet);
                }
                catch { }
            }
            
            // Give time for messages to be sent
            System.Threading.Thread.Sleep(2000);
            
            // Stop console
            ConsoleCommandManager.Instance.StopConsole();
            
            // Exit application
            Environment.Exit(0);
            
            return CommandResult.SuccessResult("Server stopping...");
        }

        public string GetHelp() => "Stop the server gracefully";
    }

    public class RestartServerCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            return CommandResult.ErrorResult("Server restart not implemented. Please stop and manually restart the server.");
        }

        public string GetHelp() => "Restart the server (not implemented)";
    }

    public class ClearConsoleCommand : IConsoleCommand
    {
        public CommandResult Execute(string[] args)
        {
            Console.Clear();
            Console.WriteLine("🖥️  Server Console");
            Console.WriteLine("📋 Type 'help' for available commands");
            Console.WriteLine();
            return CommandResult.SuccessResult("");
        }

        public string GetHelp() => "Clear the console screen";
    }

    #endregion

    #region Data Transfer Objects

    [ProtoBuf.ProtoContract]
    public class ServerBroadcast
    {
        [ProtoBuf.ProtoMember(1)]
        public string Message { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string Type { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public DateTime Timestamp { get; set; }
    }

    #endregion
} 