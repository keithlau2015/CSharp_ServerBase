using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace GameServer.Utility
{
    /// <summary>
    /// Network utility to handle firewall, permissions, and port configuration
    /// </summary>
    public static class NetworkHelper
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Check if the application is running with administrator/root privileges
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                if (IsWindows)
                {
                    using (var identity = WindowsIdentity.GetCurrent())
                    {
                        var principal = new WindowsPrincipal(identity);
                        return principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
                else
                {
                    // Check if running as root on Unix systems
                    return Environment.UserName == "root" || 
                           Process.GetCurrentProcess().Id == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a port is available for use
        /// </summary>
        public static bool IsPortAvailable(int port)
        {
            TcpListener? tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        /// <summary>
        /// Find an available port starting from the specified port
        /// </summary>
        public static int FindAvailablePort(int startPort = 8080, int maxPort = 8200)
        {
            for (int port = startPort; port <= maxPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1; // No available port found
        }

        /// <summary>
        /// Get the local IP address of the machine
        /// </summary>
        public static string GetLocalIPAddress()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Automatically configure Windows Firewall for the game server
        /// </summary>
        public static async Task<bool> ConfigureWindowsFirewall(int port, string applicationPath, string ruleName = "GameServer")
        {
            if (!IsWindows)
            {
                Debug.DebugUtility.DebugLog("Windows firewall configuration skipped (not Windows)");
                return true;
            }

            try
            {
                Debug.DebugUtility.DebugLog($"Configuring Windows Firewall for port {port}...");

                // Remove existing rules first
                await RemoveFirewallRule(ruleName);

                // Add inbound rule for the port
                string inboundCommand = $"advfirewall firewall add rule name=\"{ruleName}_Inbound\" " +
                                       $"dir=in action=allow protocol=TCP localport={port}";

                // Add outbound rule for the port
                string outboundCommand = $"advfirewall firewall add rule name=\"{ruleName}_Outbound\" " +
                                        $"dir=out action=allow protocol=TCP localport={port}";

                // Add application rule
                string appCommand = $"advfirewall firewall add rule name=\"{ruleName}_App\" " +
                                   $"dir=in action=allow program=\"{applicationPath}\"";

                bool success = true;
                success &= await RunNetshCommand(inboundCommand);
                success &= await RunNetshCommand(outboundCommand);
                success &= await RunNetshCommand(appCommand);

                if (success)
                {
                    Debug.DebugUtility.DebugLog("✅ Windows Firewall configured successfully");
                }
                else
                {
                    Debug.DebugUtility.WarningLog("⚠️ Some firewall rules may not have been created");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to configure Windows Firewall: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove firewall rules
        /// </summary>
        public static async Task<bool> RemoveFirewallRule(string ruleName)
        {
            if (!IsWindows) return true;

            try
            {
                await RunNetshCommand($"advfirewall firewall delete rule name=\"{ruleName}_Inbound\"");
                await RunNetshCommand($"advfirewall firewall delete rule name=\"{ruleName}_Outbound\"");
                await RunNetshCommand($"advfirewall firewall delete rule name=\"{ruleName}_App\"");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check and request administrator privileges if needed
        /// </summary>
        public static bool EnsureAdminPrivileges(string applicationPath)
        {
            if (IsRunningAsAdmin())
            {
                return true;
            }

            try
            {
                if (IsWindows)
                {
                    Console.WriteLine("🔐 Administrator privileges required for firewall configuration.");
                    Console.WriteLine("Would you like to restart with admin privileges? (y/n)");
                    
                    var response = Console.ReadKey();
                    Console.WriteLine();
                    
                    if (response.KeyChar == 'y' || response.KeyChar == 'Y')
                    {
                        // Restart application with admin privileges
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = applicationPath,
                            UseShellExecute = true,
                            Verb = "runas" // Request administrator privileges
                        };

                        Process.Start(startInfo);
                        Environment.Exit(0); // Exit current instance
                    }
                }
                else
                {
                    Debug.DebugUtility.WarningLog("⚠️ Root privileges may be required for firewall configuration");
                    Console.WriteLine("Try running with: sudo ./GameServer");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Failed to request admin privileges: {ex.Message}");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Generate port forwarding instructions for common routers
        /// </summary>
        public static string GeneratePortForwardingInstructions(int port)
        {
            string localIP = GetLocalIPAddress();
            
            return $@"
🌐 PORT FORWARDING SETUP INSTRUCTIONS

To allow external players to connect to your server:

1. **Find Your Router's Admin Panel:**
   - Open web browser and go to: 192.168.1.1 or 192.168.0.1
   - Login with router admin credentials

2. **Navigate to Port Forwarding:**
   - Look for: 'Port Forwarding', 'Virtual Servers', or 'Applications'
   - Common locations: Advanced → NAT → Port Forwarding

3. **Add New Rule:**
   - Service Name: GameServer
   - Protocol: TCP
   - External Port: {port}
   - Internal Port: {port}
   - Internal IP: {localIP}
   - Enable/Active: ✓

4. **Save and Restart Router**

5. **Find Your External IP:**
   - Visit: https://whatismyipaddress.com/
   - Share this IP with external players

6. **Test Connection:**
   - Use online port checker: https://www.yougetsignal.com/tools/open-ports/
   - Enter your external IP and port {port}

⚠️ **Security Warning:**
Opening ports exposes your network. Only do this for trusted players.
Consider using VPN or dedicated game server hosting for production.

🔒 **Alternative Solutions:**
- Hamachi (Virtual LAN)
- Steam Remote Play Together
- Discord Game Activity
- Cloud gaming services
";
        }

        private static async Task<bool> RunNetshCommand(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Comprehensive network diagnostics
        /// </summary>
        public static NetworkDiagnostics RunNetworkDiagnostics(int port)
        {
            var diagnostics = new NetworkDiagnostics();
            
            try
            {
                diagnostics.IsAdminMode = IsRunningAsAdmin();
                diagnostics.LocalIP = GetLocalIPAddress();
                diagnostics.IsPortAvailable = IsPortAvailable(port);
                diagnostics.Platform = GetPlatformName();
                
                if (!diagnostics.IsPortAvailable)
                {
                    diagnostics.SuggestedPort = FindAvailablePort(port, port + 100);
                }
                
                return diagnostics;
            }
            catch (Exception ex)
            {
                diagnostics.ErrorMessage = ex.Message;
                return diagnostics;
            }
        }

        private static string GetPlatformName()
        {
            if (IsWindows) return "Windows";
            if (IsLinux) return "Linux";
            if (IsMacOS) return "macOS";
            return "Unknown";
        }
    }

    public class NetworkDiagnostics
    {
        public bool IsAdminMode { get; set; }
        public string LocalIP { get; set; }
        public bool IsPortAvailable { get; set; }
        public int SuggestedPort { get; set; }
        public string Platform { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            var result = $@"
🔍 NETWORK DIAGNOSTICS REPORT
═══════════════════════════════

Platform: {Platform}
Administrator Mode: {(IsAdminMode ? "✅ Yes" : "❌ No")}
Local IP Address: {LocalIP}
Port Available: {(IsPortAvailable ? "✅ Yes" : "❌ No")}
";

            if (!IsPortAvailable && SuggestedPort > 0)
            {
                result += $"Suggested Port: {SuggestedPort}\n";
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                result += $"Error: {ErrorMessage}\n";
            }

            return result;
        }
    }
} 