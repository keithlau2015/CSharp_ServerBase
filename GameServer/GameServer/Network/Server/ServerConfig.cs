public class ServerConfig
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int TCPPort { get; set; }
    public int UDPPort { get; set; }
    public string DatabaseUser { get; set; }
    public string DatabasePw { get; set; }

    /// <summary>
    /// Debug Level:
    /// Disable: -1
    /// Debug: 0,
    /// Warning: 1,
    /// Error: 2,
    /// </summary>
    public int DebugLevel { get; set; }
}
