using System;
using System.Threading.Tasks;
using Admin;
using GameSystem.Lobby;
using GameSystem.VoIP;

namespace Demo
{
    /// <summary>
    /// Demo showing server console command functionality
    /// </summary>
    public class ConsoleCommandDemo
    {
        public static async Task RunConsoleDemo()
        {
            Console.WriteLine("🖥️  Console Command System Demo");
            Console.WriteLine("================================");
            Console.WriteLine();

            // Wait for console to initialize
            await Task.Delay(1000);

            Console.WriteLine("📋 This server now supports real-time administrative commands!");
            Console.WriteLine();
            Console.WriteLine("🎮 Available Commands:");
            Console.WriteLine();
            
            // Show command categories
            ShowCommandCategories();
            
            Console.WriteLine();
            Console.WriteLine("💡 Try these example commands:");
            Console.WriteLine();
            
            // Show example usage
            ShowExampleUsage();
            
            Console.WriteLine();
            Console.WriteLine("⚡ Server Console is now active - Type commands at 'Server>' prompt");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();
        }

        private static void ShowCommandCategories()
        {
            Console.WriteLine("📊 Information & Status:");
            Console.WriteLine("   help, info, status, players, rooms");
            Console.WriteLine();
            
            Console.WriteLine("👥 Player Management:");
            Console.WriteLine("   player <id>, kick <id>, ban <id>, unban <id>");
            Console.WriteLine();
            
            Console.WriteLine("🏠 Room Management:");
            Console.WriteLine("   room <id>, closeroom <id>, broadcast <message>");
            Console.WriteLine();
            
            Console.WriteLine("🎤 VoIP Control:");
            Console.WriteLine("   mute <id>, unmute <id>, voip, voice");
            Console.WriteLine();
            
            Console.WriteLine("🔧 Server Control:");
            Console.WriteLine("   stop, clear");
        }

        private static void ShowExampleUsage()
        {
            Console.WriteLine("📝 Example Commands:");
            Console.WriteLine();
            
            Console.WriteLine("   ℹ️  Get Help:");
            Console.WriteLine("   help                           # Show all commands");
            Console.WriteLine("   info                           # Server information");
            Console.WriteLine("   status                         # Current server status");
            Console.WriteLine();
            
            Console.WriteLine("   👥 Player Management:");
            Console.WriteLine("   players                        # List all connected players");
            Console.WriteLine("   player abc123                  # Show player details");
            Console.WriteLine("   kick abc123 Inappropriate behavior  # Kick player with reason");
            Console.WriteLine("   ban abc123 1h Spamming         # Ban for 1 hour");
            Console.WriteLine("   ban abc123 permanent Cheating  # Permanent ban");
            Console.WriteLine("   unban abc123                  # Remove ban");
            Console.WriteLine("   banned                         # List banned players");
            Console.WriteLine();
            
            Console.WriteLine("   🏠 Room Management:");
            Console.WriteLine("   rooms                          # List all rooms");
            Console.WriteLine("   room room123                   # Show room details");
            Console.WriteLine("   closeroom room123              # Close a room");
            Console.WriteLine("   broadcast Server restart in 5 minutes  # Message all players");
            Console.WriteLine();
            
            Console.WriteLine("   🎤 VoIP Management:");
            Console.WriteLine("   mute abc123                    # Mute player's microphone");
            Console.WriteLine("   unmute abc123                 # Unmute player");
            Console.WriteLine("   voip                           # VoIP system status");
            Console.WriteLine("   voice                          # List voice channels");
            Console.WriteLine();
            
            Console.WriteLine("   🔧 Server Control:");
            Console.WriteLine("   clear                          # Clear console");
            Console.WriteLine("   stop                           # Shutdown server");
        }

        /// <summary>
        /// Simulate some administrative scenarios for testing
        /// </summary>
        public static async Task SimulateAdminScenarios()
        {
            Console.WriteLine("🧪 Simulating Administrative Scenarios...");
            Console.WriteLine();

            var commandManager = ConsoleCommandManager.Instance;
            
            // Wait for some players to connect (simulate)
            await Task.Delay(3000);
            
            // Scenario 1: List players and rooms
            Console.WriteLine("📊 Scenario 1: Server Status Check");
            commandManager.ProcessCommand("status");
            await Task.Delay(1000);
            
            // Scenario 2: Show available rooms
            Console.WriteLine("🏠 Scenario 2: Room Management");
            commandManager.ProcessCommand("rooms");
            await Task.Delay(1000);
            
            // Scenario 3: VoIP status
            Console.WriteLine("🎤 Scenario 3: VoIP System Check");
            commandManager.ProcessCommand("voip");
            await Task.Delay(1000);
            
            // Show that admin can monitor activity in real-time
            Console.WriteLine("👁️  Real-time monitoring is now active!");
            Console.WriteLine("   Administrators can use commands to:");
            Console.WriteLine("   • Monitor player activity and voice chat");
            Console.WriteLine("   • Manage problematic players instantly");
            Console.WriteLine("   • Control room access and behavior");
            Console.WriteLine("   • Broadcast important messages");
            Console.WriteLine();
        }

        /// <summary>
        /// Setup event handlers to show admin notifications
        /// </summary>
        public static void SetupAdminNotifications()
        {
            var commandManager = ConsoleCommandManager.Instance;
            
            // Monitor command execution
            commandManager.OnCommandExecuted += (command) =>
            {
                Console.WriteLine($"🔧 Admin executed: {command}");
            };
            
            // Monitor player kicks
            commandManager.OnPlayerKicked += (playerId, reason) =>
            {
                Console.WriteLine($"👮 Player kicked: {playerId} - {reason}");
                
                // Broadcast to other players (optional)
                var players = LobbyManager.Instance.GetAllPlayers();
                foreach (var player in players)
                {
                    if (player.UID != playerId)
                    {
                        try
                        {
                            var notification = new PlayerKickNotification
                            {
                                KickedPlayerId = playerId,
                                Reason = reason,
                                Timestamp = DateTime.UtcNow
                            };
                            
                            var packet = new Network.Packet("PlayerKickNotification");
                            packet.Write(notification);
                            player.NetClient?.Send(packet);
                        }
                        catch { }
                    }
                }
            };
            
            // Monitor player bans
            commandManager.OnPlayerBanned += (playerId, bannedUntil, reason) =>
            {
                string duration = bannedUntil?.ToString("yyyy-MM-dd HH:mm:ss") ?? "permanent";
                Console.WriteLine($"🚫 Player banned: {playerId} until {duration} - {reason}");
            };
            
            // Monitor lobby events for admin awareness
            LobbyManager.Instance.OnPlayerJoinedRoom += (player, room) =>
            {
                Console.WriteLine($"🏠 {player.Name} joined room '{room.Name}'");
            };
            
            LobbyManager.Instance.OnPlayerLeftRoom += (player, room) =>
            {
                Console.WriteLine($"🚪 {player.Name} left room '{room.Name}'");
            };
            
            // Monitor VoIP events
            try
            {
                VoIPManager.Instance.OnPlayerStartedTalking += (roomId, playerId) =>
                {
                    var player = LobbyManager.Instance.GetPlayer(playerId);
                    Console.WriteLine($"🎤 {player?.Name ?? playerId} started talking in room");
                };
                
                VoIPManager.Instance.OnPlayerStoppedTalking += (roomId, playerId) =>
                {
                    var player = LobbyManager.Instance.GetPlayer(playerId);
                    Console.WriteLine($"🔇 {player?.Name ?? playerId} stopped talking");
                };
            }
            catch
            {
                Console.WriteLine("⚠️  VoIP monitoring not available");
            }
            
            Console.WriteLine("📡 Admin monitoring system activated!");
            Console.WriteLine("   All player activity will be logged for administrators");
            Console.WriteLine();
        }

        /// <summary>
        /// Demo of automated admin actions
        /// </summary>
        public static async Task DemoAutomatedModeration()
        {
            Console.WriteLine("🤖 Automated Moderation Demo");
            Console.WriteLine("============================");
            
            // This would typically be triggered by actual player behavior
            Console.WriteLine("🔍 Monitoring for problematic behavior...");
            
            await Task.Delay(2000);
            
            // Simulate detecting spam
            Console.WriteLine("⚠️  Spam detected from player 'BadPlayer123'");
            Console.WriteLine("🤖 Auto-moderating: Issuing warning...");
            
            // In real implementation, this would be triggered by behavior detection
            var commandManager = ConsoleCommandManager.Instance;
            
            // Simulate admin response
            await Task.Delay(1000);
            Console.WriteLine("👮 Admin action: Muting spammer");
            commandManager.ProcessCommand("mute BadPlayer123");
            
            await Task.Delay(2000);
            Console.WriteLine("⏰ Warning period expired");
            Console.WriteLine("👮 Admin action: Unmuting player");
            commandManager.ProcessCommand("unmute BadPlayer123");
            
            Console.WriteLine("✅ Automated moderation complete");
            Console.WriteLine("   Real servers can implement:");
            Console.WriteLine("   • Automatic spam detection and muting");
            Console.WriteLine("   • Behavior analysis and warnings");
            Console.WriteLine("   • Escalation to human moderators");
            Console.WriteLine("   • Appeal systems for banned players");
            Console.WriteLine();
        }
    }

    #region Notification Data Structures

    [ProtoBuf.ProtoContract]
    public class PlayerKickNotification
    {
        [ProtoBuf.ProtoMember(1)]
        public string KickedPlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string Reason { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public DateTime Timestamp { get; set; }
    }

    [ProtoBuf.ProtoContract]
    public class PlayerBanNotification
    {
        [ProtoBuf.ProtoMember(1)]
        public string BannedPlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string Reason { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public DateTime? BannedUntil { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public DateTime Timestamp { get; set; }
    }

    #endregion
} 