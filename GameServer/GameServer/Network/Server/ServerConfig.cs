public class ServerConfig
{
    // Server Identity
    public int ID { get; set; } = 0;
    public string Name { get; set; } = "GameServer";
    
    // Network Configuration
    public int TCPPort { get; set; } = 45000;
    public int UDPPort { get; set; } = 45001;
    public int Port { get; set; } = 8080;  // For compatibility with ServerLauncher
    public int MaxPlayers { get; set; } = 1000;
    
    // Database Configuration
    public string DatabaseUser { get; set; } = "";
    public string DatabasePw { get; set; } = "";
    public string DatabaseType { get; set; } = "EncryptedBinary";
    public string DataDirectory { get; set; } = "./GameData";
    public string EncryptionKey { get; set; } = "DefaultGameServerKey2024!";

    // Network Security
    public bool ConfigureFirewall { get; set; } = false;
    public bool ShowPortForwarding { get; set; } = false;
    
    // Server Control
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Debug Level:
    /// Disable: -1
    /// Debug: 0,
    /// Warning: 1,
    /// Error: 2,
    /// </summary>
    public int DebugLevel { get; set; } = 1;

    // Helper method to sync Port with TCPPort for compatibility
    public void SyncPorts()
    {
        if (Port != 8080 && TCPPort == 45000) // If Port was changed but TCPPort wasn't
        {
            TCPPort = Port;
            UDPPort = Port + 1;
        }
        else if (TCPPort != 45000 && Port == 8080) // If TCPPort was changed but Port wasn't
        {
            Port = TCPPort;
        }
    }
}
