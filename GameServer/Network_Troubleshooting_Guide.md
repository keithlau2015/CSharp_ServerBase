# 🔥 Network Troubleshooting Guide

Comprehensive guide to resolve firewall, permissions, and port forwarding issues for the C# Game Server.

## 🚨 Quick Solutions

### Problem: "Port in Use" Error
```bash
# Check what's using the port
netstat -ano | findstr :8080

# Use automatic port detection
GameServer.exe --port 8080  # Will auto-switch if needed
```

### Problem: "Administrator Required" Error
```bash
# Windows: Run as Administrator
Right-click GameServer.exe → "Run as administrator"

# Or use the admin launcher
run_server_admin.bat

# Linux/Mac: Use sudo
sudo ./GameServer
```

### Problem: "Firewall Blocking" Error
```bash
# Automatic firewall configuration
GameServer.exe --configure-firewall

# Manual Windows firewall
GameServer.exe --show-port-forwarding
```

## 🔍 Detailed Diagnostics

### Check Network Status
```bash
# Run diagnostics
GameServer.exe --help

# Check if port is available
telnet localhost 8080

# Check firewall status (Windows)
netsh advfirewall show allprofiles state
```

### Get Network Information
```bash
# Show port forwarding instructions
GameServer.exe --show-port-forwarding

# Check local IP
ipconfig  # Windows
ifconfig  # Linux/Mac
```

## 🛠️ Platform-Specific Solutions

### 🪟 Windows Solutions

#### Firewall Configuration
```bash
# Automatic (requires admin)
GameServer.exe --configure-firewall

# Manual Windows Firewall Steps:
# 1. Windows Security → Firewall & network protection
# 2. Advanced settings → Inbound Rules → New Rule
# 3. Port → TCP → 8080 → Allow
# 4. Repeat for Outbound Rules
```

#### Administrator Privileges
```batch
:: Check if running as admin
net session >nul 2>&1

:: Request admin automatically
powershell -Command "Start-Process 'GameServer.exe' -Verb RunAs"

:: Use the provided admin launcher
run_server_admin.bat
```

#### Port Issues
```cmd
:: Find process using port
netstat -ano | findstr :8080
tasklist /FI "PID eq 1234"

:: Kill process if needed
taskkill /PID 1234 /F

:: Use different port
GameServer.exe --port 9000
```

### 🐧 Linux Solutions

#### Firewall Configuration (UFW)
```bash
# Enable UFW
sudo ufw enable

# Allow port
sudo ufw allow 8080/tcp

# Check status
sudo ufw status
```

#### Firewall Configuration (iptables)
```bash
# Allow port
sudo iptables -A INPUT -p tcp --dport 8080 -j ACCEPT

# Save rules (Ubuntu/Debian)
sudo iptables-save > /etc/iptables/rules.v4

# Save rules (CentOS/RHEL)
sudo service iptables save
```

#### Permissions
```bash
# Run with elevated privileges
sudo ./GameServer --configure-firewall

# Make executable
chmod +x GameServer

# Check port permissions
sudo netstat -tulpn | grep :8080
```

### 🍎 macOS Solutions

#### Firewall Configuration
```bash
# Check firewall status
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# Add application to firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add GameServer
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp GameServer

# Manual: System Preferences → Security & Privacy → Firewall
```

#### Permissions
```bash
# Grant permission in Security settings
# System Preferences → Security & Privacy → General
# Click "Allow Anyway" when prompted

# Run with sudo
sudo ./GameServer --configure-firewall
```

## 🌐 Router/Port Forwarding

### Automatic Instructions
```bash
# Get detailed port forwarding guide
GameServer.exe --show-port-forwarding
```

### Manual Router Configuration

#### 1. Find Router IP
```bash
# Windows
ipconfig | findstr "Default Gateway"

# Linux/Mac
route -n | grep '^0.0.0.0'
```

#### 2. Access Router Admin Panel
- Open browser: `http://192.168.1.1` or `http://192.168.0.1`
- Login with router credentials
- Look for: Port Forwarding, Virtual Servers, or NAT

#### 3. Add Port Forwarding Rule
```
Service Name: GameServer
Protocol: TCP
External Port: 8080
Internal Port: 8080
Internal IP: [Your PC's local IP]
Enable: ✓
```

#### 4. Test External Connection
```bash
# Get external IP
curl ifconfig.me

# Test port (online tool)
# Visit: https://www.yougetsignal.com/tools/open-ports/
# Enter your external IP and port 8080
```

## 🔧 Unity Integration Fixes

### GameServerManager Issues

#### Server Won't Start
```csharp
// Check executable path
Debug.Log($"Looking for server at: {GetServerExecutablePath()}");

// Check permissions
if (!File.Exists(serverExecutablePath))
{
    Debug.LogError("Server executable not found!");
}
```

#### Firewall Configuration in Unity
```csharp
// Enable firewall configuration
[SerializeField] private bool configureFirewall = true;

// Monitor server process
serverManager.OnServerError += (error) => {
    Debug.LogError($"Server Error: {error}");
    // Handle firewall/permission errors
};
```

### Common Unity Errors

#### "Access Denied" Error
```csharp
// Solution: Run Unity as Administrator
// Or disable firewall configuration
configureFirewall = false;
```

#### "Port Already in Use" Error
```csharp
// Solution: Enable auto port detection
autoDetectPort = true;

// Or specify different port
serverPort = 9000;
```

## 🛡️ Security Considerations

### Local Development
- ✅ Use firewall rules for specific ports
- ✅ Use localhost/LAN connections only
- ❌ Don't expose to internet without proper security

### Port Forwarding Risks
- ⚠️ Only forward ports for trusted players
- ⚠️ Use strong encryption keys
- ⚠️ Monitor connection logs
- ⚠️ Close ports when not needed

### Alternative Solutions
```bash
# VPN solutions (safer than port forwarding)
- Hamachi (LogMeIn)
- ZeroTier
- Tailscale

# Steam networking
- Steam Remote Play Together
- Steam Networking API

# Cloud hosting
- AWS GameLift
- Google Cloud Game Servers
- Azure PlayFab
```

## 📊 Monitoring & Diagnostics

### Real-time Monitoring
```bash
# Monitor network connections
netstat -an | findstr :8080

# Monitor server logs
tail -f server.log

# Monitor system resources
Resource Monitor → Network tab
```

### Performance Diagnostics
```bash
# Check CPU/Memory usage
Task Manager → Processes → GameServer.exe

# Network performance
ping 127.0.0.1
iperf3 -s  # Server
iperf3 -c 127.0.0.1  # Client
```

## 🆘 Emergency Solutions

### Server Won't Start at All
1. **Check .NET Runtime**: `dotnet --version`
2. **Verify executable**: File exists and has correct permissions
3. **Try different port**: `--port 9000`
4. **Disable firewall**: `--no-firewall`
5. **Run as admin**: Right-click → "Run as administrator"

### Can't Connect from Other Machines
1. **Check local connection first**: `telnet 127.0.0.1 8080`
2. **Test LAN connection**: `telnet [local-ip] 8080`
3. **Verify firewall rules**: Windows Firewall settings
4. **Check router settings**: Port forwarding configuration
5. **Test with online tool**: Port checker websites

### Performance Issues
1. **Reduce max players**: `--maxplayers 50`
2. **Use optimized database**: `--database OptimizedEncryptedBinary`
3. **Monitor resources**: Task Manager
4. **Check network latency**: `ping [server-ip]`
5. **Restart router**: Unplug for 30 seconds

## 🎯 Best Practices

### Development
- ✅ Test locally first (127.0.0.1)
- ✅ Use LAN testing before internet
- ✅ Keep firewall logs enabled
- ✅ Document working configurations

### Production
- ✅ Use dedicated server hosting
- ✅ Implement proper authentication
- ✅ Monitor server health
- ✅ Have backup/failover plans
- ❌ Don't use home internet for production

### Troubleshooting Workflow
1. **Local test**: Can you connect to 127.0.0.1?
2. **LAN test**: Can other devices on your network connect?
3. **Firewall test**: Are rules configured correctly?
4. **Router test**: Is port forwarding working?
5. **Internet test**: Can external users connect?

---

## 📞 Getting Help

### Built-in Diagnostics
```bash
# Run comprehensive diagnostics
GameServer.exe --help

# Network information
GameServer.exe --show-port-forwarding
```

### Community Resources
- 🔗 [Port Checker Tool](https://www.yougetsignal.com/tools/open-ports/)
- 🔗 [What Is My IP](https://whatismyipaddress.com/)
- 🔗 [Windows Firewall Guide](https://support.microsoft.com/en-us/windows/turn-microsoft-defender-firewall-on-or-off-ec0844f7-aebd-0583-67fe-601ecf5d774f)

### Log Files Location
```
Windows: %APPDATA%/GameServer/logs/
Linux:   ~/.local/share/GameServer/logs/
macOS:   ~/Library/Application Support/GameServer/logs/
```

Remember: Most network issues can be resolved with proper firewall configuration and administrator privileges! 🚀 