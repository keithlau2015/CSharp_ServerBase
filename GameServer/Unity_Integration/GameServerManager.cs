using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameServer.Unity
{
    /// <summary>
    /// Unity component to manage the external C# Game Server
    /// Place this script on a GameObject in your Unity scene
    /// </summary>
    public class GameServerManager : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private string serverExecutablePath = "GameServer.exe";
        [SerializeField] private int tcpPort = 439500;
        [SerializeField] private int udpPort = 539500;
        [SerializeField] private int maxPlayers = 1000;
        [SerializeField] private string serverName = "Unity Game Server";
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool autoStopOnDestroy = true;
        
        [Header("Debug Configuration")]
        [SerializeField] private bool enableDebugMode = true;
        [SerializeField] private string debugLevel = "Info"; // Debug, Info, Warning, Error
        [SerializeField] private bool runDemo = true;
        [SerializeField] private bool enableAdminConsole = true;
        
        [Header("Network Configuration")]
        [SerializeField] private bool configureFirewall = false;
        [SerializeField] private bool showPortForwarding = false;
        [SerializeField] private bool autoDetectPort = true;
        
        [Header("VoIP Configuration")]
        [SerializeField] private bool enableVoIP = true;
        [SerializeField] private int voipSampleRate = 48000;
        [SerializeField] private string voipCodec = "Opus";
        [SerializeField] private bool voip3DPositional = true;
        
        [Header("Monitoring")]
        [SerializeField] private float healthCheckInterval = 5f;
        [SerializeField] private bool showServerConsole = true;
        [SerializeField] private bool autoRestart = false;
        [SerializeField] private int maxRestartAttempts = 3;
        
        private Process serverProcess;
        private bool isServerRunning = false;
        private float lastHealthCheck = 0f;
        private int restartAttempts = 0;
        
        // Events
        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnServerError;
        public event Action<int> OnPlayerCountChanged;
        
        public bool IsServerRunning => isServerRunning && serverProcess != null && !serverProcess.HasExited;
        public int TCPPort => tcpPort;
        public int UDPPort => udpPort;
        public string ServerAddress => $"127.0.0.1";
        public string TCPAddress => $"{ServerAddress}:{tcpPort}";
        public string UDPAddress => $"{ServerAddress}:{udpPort}";

        private void Awake()
        {
            if (autoStartOnAwake)
            {
                StartServer();
            }
        }

        private void Update()
        {
            // Monitor server health
            if (Time.time - lastHealthCheck > healthCheckInterval)
            {
                CheckServerHealth();
                lastHealthCheck = Time.time;
            }
        }

        private void OnDestroy()
        {
            if (autoStopOnDestroy && IsServerRunning)
            {
                StopServer();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Handle application pause (mobile)
            if (pauseStatus && IsServerRunning)
            {
                UnityEngine.Debug.Log("Application paused, keeping server running");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Handle application focus change
            if (!hasFocus && IsServerRunning)
            {
                UnityEngine.Debug.Log("Application lost focus, keeping server running");
            }
        }

        /// <summary>
        /// Start the external game server
        /// </summary>
        public void StartServer()
        {
            if (IsServerRunning)
            {
                UnityEngine.Debug.LogWarning("Server is already running!");
                return;
            }

            try
            {
                string fullPath = GetServerExecutablePath();
                
                if (!File.Exists(fullPath))
                {
                    string error = $"Server executable not found at: {fullPath}";
                    UnityEngine.Debug.LogError(error);
                    OnServerError?.Invoke(error);
                    return;
                }

                // Build command line arguments
                string arguments = BuildCommandLineArguments();
                
                UnityEngine.Debug.Log($"Starting C# Game Server: {fullPath}");
                UnityEngine.Debug.Log($"TCP Port: {tcpPort}, UDP Port: {udpPort}");
                UnityEngine.Debug.Log($"Arguments: {arguments}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = arguments,
                    UseShellExecute = showServerConsole,
                    CreateNoWindow = !showServerConsole,
                    RedirectStandardOutput = !showServerConsole,
                    RedirectStandardError = !showServerConsole,
                    RedirectStandardInput = !showServerConsole,
                    WorkingDirectory = Path.GetDirectoryName(fullPath)
                };

                serverProcess = Process.Start(startInfo);
                
                if (serverProcess != null)
                {
                    isServerRunning = true;
                    restartAttempts = 0;
                    UnityEngine.Debug.Log($"✅ C# Game Server started successfully! PID: {serverProcess.Id}");
                    UnityEngine.Debug.Log($"🌐 Players can connect via TCP: {TCPAddress} and UDP: {UDPAddress}");
                    if (enableVoIP)
                    {
                        UnityEngine.Debug.Log($"🎤 VoIP system enabled with {voipCodec} codec");
                    }
                    if (enableAdminConsole)
                    {
                        UnityEngine.Debug.Log($"⚙️ Admin console available for server management");
                    }
                    
                    OnServerStarted?.Invoke();
                    
                    // Monitor process in background
                    _ = MonitorServerProcessAsync();
                }
                else
                {
                    string error = "Failed to start server process";
                    UnityEngine.Debug.LogError(error);
                    OnServerError?.Invoke(error);
                }
            }
            catch (Exception ex)
            {
                string error = $"Error starting server: {ex.Message}";
                UnityEngine.Debug.LogError(error);
                OnServerError?.Invoke(error);
            }
        }

        /// <summary>
        /// Stop the external game server
        /// </summary>
        public void StopServer()
        {
            if (!IsServerRunning)
            {
                UnityEngine.Debug.LogWarning("Server is not running!");
                return;
            }

            try
            {
                UnityEngine.Debug.Log("🛑 Stopping C# Game Server...");
                
                // Try graceful shutdown first using admin console command
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    try
                    {
                        // Send 'stop' command to admin console
                        if (!showServerConsole && serverProcess.StandardInput != null)
                        {
                            serverProcess.StandardInput.WriteLine("stop");
                            serverProcess.StandardInput.Flush();
                        }
                        
                        // Wait a bit for graceful shutdown
                        if (!serverProcess.WaitForExit(10000)) // 10 seconds for graceful shutdown
                        {
                            // Force kill if graceful shutdown failed
                            UnityEngine.Debug.LogWarning("Graceful shutdown timeout, force closing server");
                            serverProcess.Kill();
                        }
                    }
                    catch
                    {
                        // If StandardInput is not available, just kill the process
                        serverProcess.Kill();
                    }
                }

                isServerRunning = false;
                serverProcess = null;
                UnityEngine.Debug.Log("✅ C# Game Server stopped successfully");
                OnServerStopped?.Invoke();
            }
            catch (Exception ex)
            {
                string error = $"Error stopping server: {ex.Message}";
                UnityEngine.Debug.LogError(error);
                OnServerError?.Invoke(error);
            }
        }

        /// <summary>
        /// Restart the server
        /// </summary>
        public void RestartServer()
        {
            UnityEngine.Debug.Log("🔄 Restarting C# Game Server...");
            StopServer();
            
            // Wait a moment before restarting
            StartCoroutine(DelayedStart(3f));
        }

        private System.Collections.IEnumerator DelayedStart(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartServer();
        }

        /// <summary>
        /// Check if server is responding to TCP connections
        /// </summary>
        public async Task<bool> IsServerResponding()
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = client.ConnectAsync(ServerAddress, tcpPort);
                    var timeoutTask = Task.Delay(3000);
                    
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask && client.Connected)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Connection failed
            }
            
            return false;
        }

        /// <summary>
        /// Send admin command to the server
        /// </summary>
        public void SendAdminCommand(string command)
        {
            if (!IsServerRunning || showServerConsole)
            {
                UnityEngine.Debug.LogWarning("Cannot send admin command: Server not running or console is visible");
                return;
            }

            try
            {
                if (serverProcess?.StandardInput != null)
                {
                    serverProcess.StandardInput.WriteLine(command);
                    serverProcess.StandardInput.Flush();
                    UnityEngine.Debug.Log($"📡 Sent admin command: {command}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to send admin command: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current player count (would need server API integration)
        /// </summary>
        public async Task<int> GetPlayerCount()
        {
            // This would require implementing a simple HTTP API in the server
            // For now, return -1 to indicate unavailable
            return -1;
        }

        private string GetServerExecutablePath()
        {
            // Try different locations for the server executable
            string[] possiblePaths = {
                serverExecutablePath,
                Path.Combine(Application.dataPath, "..", serverExecutablePath),
                Path.Combine(Application.streamingAssetsPath, serverExecutablePath),
                Path.Combine(Application.persistentDataPath, serverExecutablePath),
                Path.Combine(Application.dataPath, "..", "GameServer", "bin", "Release", "net6.0", serverExecutablePath),
                Path.Combine(Application.dataPath, "..", "GameServer", "bin", "Debug", "net6.0", serverExecutablePath)
            };

            foreach (string path in possiblePaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    UnityEngine.Debug.Log($"Found server executable at: {fullPath}");
                    return fullPath;
                }
            }

            UnityEngine.Debug.LogWarning($"Server executable not found. Tried paths: {string.Join(", ", possiblePaths)}");
            return serverExecutablePath; // Return original if not found (will cause error)
        }

        private string BuildCommandLineArguments()
        {
            // Build arguments for the actual C# Game Server
            string args = "";
            
            // The current C# server doesn't use command line args, it uses appconfig.json
            // But we can add environment variables or config file modification here
            
            if (enableDebugMode)
            {
                args += " --debug";
            }
            
            if (!runDemo)
            {
                args += " --no-demo";
            }

            // Note: The actual server configuration is in appconfig.json
            // We could dynamically modify that file here if needed
            
            return args.Trim();
        }

        /// <summary>
        /// Modify the server configuration file before startup
        /// </summary>
        private void UpdateServerConfig()
        {
            try
            {
                string configPath = Path.Combine(Path.GetDirectoryName(GetServerExecutablePath()), "Config", "appconfig.json");
                
                if (File.Exists(configPath))
                {
                    string configJson = File.ReadAllText(configPath);
                    
                    // Parse and modify config (requires Newtonsoft.Json or similar)
                    // For now, just log that we found the config
                    UnityEngine.Debug.Log($"Found server config at: {configPath}");
                    
                    // TODO: Implement JSON modification to update TCP/UDP ports
                    // This would allow Unity to override the appconfig.json settings
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Could not update server config: {ex.Message}");
            }
        }

        private async Task MonitorServerProcessAsync()
        {
            while (serverProcess != null && !serverProcess.HasExited)
            {
                await Task.Delay(1000);
            }

            // Server process has exited
            if (isServerRunning)
            {
                isServerRunning = false;
                UnityEngine.Debug.LogWarning("🚨 C# Game Server process has exited unexpectedly");
                
                // Auto-restart if enabled
                if (autoRestart && restartAttempts < maxRestartAttempts)
                {
                    restartAttempts++;
                    UnityEngine.Debug.Log($"🔄 Auto-restarting server (attempt {restartAttempts}/{maxRestartAttempts})");
                    
                    // Wait a bit before restarting
                    await Task.Delay(5000);
                    StartServer();
                }
                else
                {
                    OnServerStopped?.Invoke();
                }
            }
        }

        private void CheckServerHealth()
        {
            if (IsServerRunning)
            {
                // Check if process is still alive
                if (serverProcess.HasExited)
                {
                    isServerRunning = false;
                    UnityEngine.Debug.LogWarning("🚨 Server process has exited");
                    OnServerStopped?.Invoke();
                    return;
                }

                // Additional health checks could be added here
                // For example, TCP connection test, player count monitoring, etc.
            }
        }

        // Unity Inspector buttons for testing
        [ContextMenu("Start Server")]
        private void StartServerFromInspector()
        {
            StartServer();
        }

        [ContextMenu("Stop Server")]
        private void StopServerFromInspector()
        {
            StopServer();
        }

        [ContextMenu("Restart Server")]
        private void RestartServerFromInspector()
        {
            RestartServer();
        }

        [ContextMenu("Test Connection")]
        private async void TestConnectionFromInspector()
        {
            bool isResponding = await IsServerResponding();
            UnityEngine.Debug.Log($"Server responding: {isResponding}");
        }

        [ContextMenu("Send Test Command")]
        private void SendTestCommandFromInspector()
        {
            SendAdminCommand("players");
        }
    }
} 