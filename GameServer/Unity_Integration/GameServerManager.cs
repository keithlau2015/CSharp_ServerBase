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
        [SerializeField] private int serverPort = 8080;
        [SerializeField] private int maxPlayers = 100;
        [SerializeField] private string databaseType = "EncryptedBinary";
        [SerializeField] private string dataDirectory = "./GameData";
        [SerializeField] private string encryptionKey = "UnityGameServerKey2024!";
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool autoStopOnDestroy = true;
        
        [Header("Network Configuration")]
        [SerializeField] private bool configureFirewall = false;
        [SerializeField] private bool showPortForwarding = false;
        [SerializeField] private bool autoDetectPort = true;
        
        [Header("Monitoring")]
        [SerializeField] private float healthCheckInterval = 5f;
        [SerializeField] private bool showServerConsole = true;
        
        private Process serverProcess;
        private bool isServerRunning = false;
        private float lastHealthCheck = 0f;
        
        // Events
        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnServerError;
        
        public bool IsServerRunning => isServerRunning && serverProcess != null && !serverProcess.HasExited;
        public int ServerPort => serverPort;
        public string ServerAddress => $"127.0.0.1:{serverPort}";

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
                
                UnityEngine.Debug.Log($"Starting server: {fullPath} {arguments}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = arguments,
                    UseShellExecute = showServerConsole,
                    CreateNoWindow = !showServerConsole,
                    RedirectStandardOutput = !showServerConsole,
                    RedirectStandardError = !showServerConsole,
                    WorkingDirectory = Path.GetDirectoryName(fullPath)
                };

                serverProcess = Process.Start(startInfo);
                
                if (serverProcess != null)
                {
                    isServerRunning = true;
                    UnityEngine.Debug.Log($"✅ Server started successfully! PID: {serverProcess.Id}");
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
                UnityEngine.Debug.Log("Stopping server...");
                
                // Try graceful shutdown first (send 'q' to console)
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    try
                    {
                        serverProcess.StandardInput?.WriteLine("q");
                        
                        // Wait a bit for graceful shutdown
                        if (!serverProcess.WaitForExit(5000))
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
                UnityEngine.Debug.Log("✅ Server stopped successfully");
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
            UnityEngine.Debug.Log("Restarting server...");
            StopServer();
            
            // Wait a moment before restarting
            StartCoroutine(DelayedStart(2f));
        }

        private System.Collections.IEnumerator DelayedStart(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartServer();
        }

        /// <summary>
        /// Check if server is responding to HTTP requests
        /// </summary>
        public async Task<bool> IsServerResponding()
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get($"http://127.0.0.1:{serverPort}/health"))
                {
                    request.timeout = 3;
                    await request.SendWebRequest();
                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetServerExecutablePath()
        {
            // Try different locations for the server executable
            string[] possiblePaths = {
                serverExecutablePath,
                Path.Combine(Application.dataPath, "..", serverExecutablePath),
                Path.Combine(Application.streamingAssetsPath, serverExecutablePath),
                Path.Combine(Application.persistentDataPath, serverExecutablePath)
            };

            foreach (string path in possiblePaths)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return serverExecutablePath; // Return original if not found (will cause error)
        }

        private string BuildCommandLineArguments()
        {
            string args = $"--port {serverPort} " +
                         $"--maxplayers {maxPlayers} " +
                         $"--database {databaseType} " +
                         $"--datadir \"{dataDirectory}\" " +
                         $"--key \"{encryptionKey}\"";

            if (configureFirewall)
            {
                args += " --configure-firewall";
            }
            else
            {
                args += " --no-firewall";
            }

            if (showPortForwarding)
            {
                args += " --show-port-forwarding";
            }

            return args;
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
                UnityEngine.Debug.LogWarning("Server process has exited unexpectedly");
                OnServerStopped?.Invoke();
            }
        }

        private void CheckServerHealth()
        {
            if (IsServerRunning)
            {
                // Could add HTTP health check here
                // For now, just check if process is still alive
                if (serverProcess.HasExited)
                {
                    isServerRunning = false;
                    UnityEngine.Debug.LogWarning("Server process has exited");
                    OnServerStopped?.Invoke();
                }
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
    }
} 