# 🎤 VoIP Integration Guide

A comprehensive guide to implementing Voice over IP (VoIP) functionality with the existing lobby and UDP room broadcasting system.

## 🚀 Overview

The VoIP system seamlessly integrates with the existing lobby infrastructure to provide:
- **Room-based Voice Chat**: Voice communication within game rooms
- **Positional Audio**: 3D spatial audio based on player positions
- **Real-time Communication**: Low-latency UDP audio transmission
- **Advanced Features**: Push-to-talk, voice activation, noise reduction
- **Quality Management**: Adaptive bitrates, compression, and quality metrics

## 🏗️ Architecture Integration

```
Existing Lobby System + VoIP Extension
├── LobbyManager (existing)
│   ├── Room Management
│   └── Player Management
├── VoIPManager (new)
│   ├── Voice Channels per Room
│   ├── Player Voice States
│   └── Audio Processing
└── UDP Broadcasting (enhanced)
    ├── Audio Packets
    ├── Voice State Updates
    └── Quality Metrics
```

## 📋 Quick Setup

### 1. Initialize VoIP with Existing Lobby

```csharp
// The VoIP system automatically integrates with LobbyManager
var lobbyManager = LobbyManager.Instance;
var voipManager = VoIPManager.Instance;

// Create room with voice chat enabled
var room = lobbyManager.CreateRoom("Voice Chat Room", 8, false);
var voiceChannel = voipManager.CreateVoiceChannel(room.Id);

// Players join room and automatically get voice capabilities
var player = lobbyManager.CreatePlayer(netClient, "PlayerName");
lobbyManager.JoinRoom(player.UID, room.Id);
```

### 2. Client VoIP Integration

```csharp
var voipClient = new VoIPClientExample();

// Connect and join room (same as before)
await voipClient.ConnectToServer("127.0.0.1", 8080);
await voipClient.JoinRoom(roomId, "PlayerName");

// Configure VoIP settings
var settings = new VoIPSettings
{
    SampleRate = 16000,
    Channels = 1,
    EnablePositionalAudio = true,
    ActivationMode = VoiceActivationMode.VoiceActivated
};
voipClient.UpdateVoIPSettings(settings);

// Start voice transmission
await voipClient.StartVoiceTransmission();
```

## 🎤 VoIP Features

### Voice Activation Modes

```csharp
public enum VoiceActivationMode
{
    PushToTalk,        // Press and hold key to talk
    VoiceActivated,    // Automatic voice detection
    OpenMic           // Always transmitting
}

// Set activation mode
voipClient.SetActivationMode(VoiceActivationMode.VoiceActivated);

// Push-to-talk controls
await voipClient.StartPushToTalk();  // Key pressed
await voipClient.StopPushToTalk();   // Key released
```

### Voice Controls

```csharp
// Mute/unmute microphone
await voipClient.ToggleMute();

// Deafen (stop hearing others)
await voipClient.ToggleDeafen();

// Volume controls
voiceState.InputVolume = 0.8f;   // Microphone sensitivity
voiceState.OutputVolume = 1.2f;  // Speaker volume
```

### Audio Quality Settings

```csharp
var settings = new VoIPSettings
{
    // Audio Quality
    SampleRate = 16000,              // 16kHz for voice (8k, 16k, 22k, 44k)
    Channels = 1,                    // Mono for voice chat
    BitsPerSample = 16,              // 16-bit audio
    BitRate = 32000,                 // 32kbps compressed
    
    // Audio Processing
    EnableNoiseReduction = true,      // Remove background noise
    EnableAutoGainControl = true,     // Normalize volume
    EnableEchoCancellation = true,    // Remove echo
    NoiseGateThreshold = 0.02f,      // 2% noise gate
    
    // Positional Audio
    EnablePositionalAudio = true,     // 3D spatial audio
    Enable3DAudio = true,            // Stereo positioning
    MinAudioDistance = 1.0f,         // Full volume distance
    MaxAudioDistance = 50.0f,        // Max hearing distance
    
    // Codec Settings
    PreferredCodec = AudioCodec.Opus, // High quality, low latency
    CompressionLevel = CompressionLevel.Medium
};
```

## 📡 Real-time Audio Transmission

### Sending Audio Data

```csharp
// Audio packet structure
public class AudioPacket
{
    public string PlayerId { get; set; }
    public byte[] AudioData { get; set; }
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public DateTime Timestamp { get; set; }
    public PlayerPosition Position { get; set; }  // For 3D audio
    public float Volume { get; set; } = 1.0f;
    public AudioCodec Codec { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsEndOfSpeech { get; set; }
}

// Send audio chunk (typically every 20ms)
var audioPacket = new AudioPacket
{
    PlayerId = playerId,
    AudioData = capturedAudioData,
    Position = currentPlayerPosition,
    Timestamp = DateTime.UtcNow
};

var packet = new Packet("AudioPacket");
packet.Write(audioPacket);
netClient.Send(packet, NetClient.SupportProtocol.UDP);
```

### Receiving Audio Data

```csharp
// Server automatically broadcasts audio to room members
public void HandleAudioReceived(AudioBroadcast audioBroadcast)
{
    var audio = audioBroadcast.AudioPacket;
    
    // Apply positional audio
    float distance = audioBroadcast.DistanceFromListener;
    float volume = audioBroadcast.VolumeMultiplier;
    
    // Play audio through speakers
    PlayAudioData(audio.AudioData, volume);
    
    Console.WriteLine($"🔊 Audio from {audio.PlayerName}: {distance:F1}m away");
}
```

## 🌍 Positional Audio System

### 3D Spatial Audio

The VoIP system automatically calculates 3D audio based on player positions:

```csharp
// Server-side positional audio calculation
private AudioPacket CalculatePositionalAudio(AudioPacket originalAudio, 
    PlayerPosition senderPos, PlayerPosition listenerPos)
{
    // Calculate distance
    float distance = CalculateDistance(senderPos, listenerPos);
    
    // Apply distance-based volume attenuation
    float volumeMultiplier = CalculateVolumeAttenuation(distance);
    
    // Apply 3D positioning (stereo panning)
    if (settings.Enable3DAudio && originalAudio.Channels >= 2)
    {
        var panValue = CalculateStereoPan(senderPos, listenerPos);
        processedAudio.AudioData = ApplyStereoPan(originalAudio.AudioData, panValue);
    }
    
    return processedAudio;
}
```

### Distance-based Volume

```csharp
// Volume attenuation based on distance
private float CalculateVolumeAttenuation(float distance)
{
    if (distance <= settings.MinAudioDistance)
        return 1.0f;  // Full volume
    
    if (distance >= settings.MaxAudioDistance)
        return 0.0f;  // No audio
    
    // Linear attenuation (can be logarithmic)
    float attenuation = 1.0f - ((distance - settings.MinAudioDistance) / 
                              (settings.MaxAudioDistance - settings.MinAudioDistance));
    
    return Math.Max(0.0f, Math.Min(1.0f, attenuation));
}
```

## 🔊 Audio Processing

### Noise Reduction

```csharp
// Simple noise gate implementation
private byte[] ApplyNoiseReduction(byte[] audioData)
{
    var threshold = settings.NoiseGateThreshold;
    var processedData = new byte[audioData.Length];
    
    for (int i = 0; i < audioData.Length; i += 2) // 16-bit samples
    {
        short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
        float normalizedSample = sample / 32768.0f;
        
        if (Math.Abs(normalizedSample) < threshold)
        {
            processedData[i] = 0;     // Remove noise
            processedData[i + 1] = 0;
        }
        else
        {
            processedData[i] = audioData[i];     // Keep signal
            processedData[i + 1] = audioData[i + 1];
        }
    }
    
    return processedData;
}
```

### Auto Gain Control

```csharp
// Automatic volume normalization
private byte[] ApplyAutoGainControl(byte[] audioData)
{
    // Find peak level
    float maxSample = 0;
    for (int i = 0; i < audioData.Length; i += 2)
    {
        short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
        maxSample = Math.Max(maxSample, Math.Abs(sample / 32768.0f));
    }
    
    // Calculate gain to normalize to ~80% max
    if (maxSample > 0.1f)
    {
        float gain = Math.Min(0.8f / maxSample, 2.0f); // Max 2x gain
        return ApplyGain(audioData, gain);
    }
    
    return audioData;
}
```

## 📊 Voice Quality Monitoring

### Quality Metrics

```csharp
// Monitor voice quality in real-time
public class VoiceQualityMetrics
{
    public float SignalToNoiseRatio { get; set; }    // Signal quality
    public float PacketLossPercentage { get; set; }  // Network quality
    public float AverageLatency { get; set; }        // Network latency
    public float JitterMs { get; set; }              // Network stability
    public int DroppedPackets { get; set; }          // Lost packets
    public string CodecUsed { get; set; }            // Audio codec
    public int BitRateKbps { get; set; }             // Current bitrate
}

// Send quality metrics to server
await voipClient.SendVoiceQualityMetrics();
```

### Adaptive Quality

```csharp
// Automatically adjust quality based on network conditions
if (metrics.PacketLossPercentage > 5.0f)
{
    // Reduce bitrate to improve reliability
    settings.BitRate = Math.Max(16000, settings.BitRate - 8000);
    Console.WriteLine($"⚠️ High packet loss, reducing bitrate to {settings.BitRate}bps");
}

if (metrics.AverageLatency > 150.0f)
{
    // Reduce buffer size for lower latency
    settings.AudioBufferMs = Math.Max(10, settings.AudioBufferMs - 5);
    Console.WriteLine($"⚠️ High latency, reducing buffer to {settings.AudioBufferMs}ms");
}
```

## 🎮 Game Integration Examples

### Battle Royale Integration

```csharp
// Proximity voice chat in battle royale
public class BattleRoyaleVoIP
{
    public async Task SetupProximityChat()
    {
        var settings = new VoIPSettings
        {
            EnablePositionalAudio = true,
            MinAudioDistance = 5.0f,    // Whisper distance
            MaxAudioDistance = 25.0f,   // Shout distance
            ActivationMode = VoiceActivationMode.VoiceActivated
        };
        
        // Players can only hear others within range
        voipManager.Settings = settings;
    }
    
    public void UpdatePlayerPosition(Player player, Vector3 newPosition)
    {
        // Update position for both game and audio
        player.UpdatePosition(newPosition.x, newPosition.y, newPosition.z);
        
        // Audio will automatically use new position for 3D calculations
    }
}
```

### Team-based Communication

```csharp
// Separate voice channels for teams
public class TeamVoIP
{
    public async Task SetupTeamChat(GameRoom room)
    {
        // Create separate voice channels for each team
        var teamAChannel = voipManager.CreateVoiceChannel($"{room.Id}_TeamA");
        var teamBChannel = voipManager.CreateVoiceChannel($"{room.Id}_TeamB");
        
        // Players only hear their team members
        foreach (var player in room.GetPlayers())
        {
            string teamChannel = player.GetTeam() == "A" ? teamAChannel.RoomId : teamBChannel.RoomId;
            // Assign player to team channel
        }
    }
}
```

### Squad Communication

```csharp
// Multiple voice channels with different ranges
public class SquadVoIP
{
    public void SetupSquadChannels()
    {
        // Close proximity (squad members nearby)
        var proximitySettings = new VoIPSettings
        {
            MinAudioDistance = 1.0f,
            MaxAudioDistance = 10.0f,
            Volume = 1.0f
        };
        
        // Radio communication (unlimited range, lower quality)
        var radioSettings = new VoIPSettings
        {
            EnablePositionalAudio = false,  // No distance limit
            SampleRate = 8000,              // Lower quality for radio effect
            Volume = 0.7f,                  // Slightly quieter
            EnableNoiseReduction = false    // Keep radio static
        };
    }
}
```

## 🔧 Advanced Configuration

### Codec Selection

```csharp
public enum AudioCodec
{
    PCM,        // Uncompressed, highest quality, most bandwidth
    Opus,       // Best overall (recommended for VoIP)
    Speex,      // Good for voice, lower bandwidth
    AAC,        // High quality, moderate bandwidth
    MP3         // Widely supported, higher latency
}

// Automatic codec selection based on network conditions
private AudioCodec SelectOptimalCodec(VoiceQualityMetrics metrics)
{
    if (metrics.PacketLossPercentage < 1.0f && metrics.AverageLatency < 50.0f)
        return AudioCodec.Opus;      // Best quality
    else if (metrics.PacketLossPercentage < 3.0f)
        return AudioCodec.Speex;     // Good reliability
    else
        return AudioCodec.MP3;       // Most reliable
}
```

### Bandwidth Management

```csharp
// Dynamic bitrate adjustment
public class BandwidthManager
{
    public void AdjustQuality(VoiceQualityMetrics metrics)
    {
        if (metrics.PacketLossPercentage > 5.0f)
        {
            // High packet loss - reduce quality
            settings.SampleRate = Math.Max(8000, settings.SampleRate - 2000);
            settings.BitRate = Math.Max(16000, settings.BitRate - 4000);
        }
        else if (metrics.PacketLossPercentage < 1.0f && metrics.AverageLatency < 50.0f)
        {
            // Good connection - increase quality if possible
            settings.SampleRate = Math.Min(22050, settings.SampleRate + 2000);
            settings.BitRate = Math.Min(64000, settings.BitRate + 4000);
        }
    }
}
```

## 🛠️ Integration with Unity

### Unity Client Integration

```csharp
// Unity MonoBehaviour for VoIP
public class UnityVoIPClient : MonoBehaviour
{
    private VoIPClientExample voipClient;
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    
    void Start()
    {
        voipClient = new VoIPClientExample();
        audioSource = GetComponent<AudioSource>();
        
        // Initialize microphone
        if (Microphone.devices.Length > 0)
        {
            microphoneClip = Microphone.Start(null, true, 1, 16000);
        }
    }
    
    void Update()
    {
        // Push-to-talk with space key
        if (Input.GetKeyDown(KeyCode.Space))
            voipClient.StartPushToTalk();
        
        if (Input.GetKeyUp(KeyCode.Space))
            voipClient.StopPushToTalk();
        
        // Mute with M key
        if (Input.GetKeyDown(KeyCode.M))
            voipClient.ToggleMute();
    }
    
    public void OnAudioDataReceived(AudioBroadcast audioBroadcast)
    {
        // Convert byte array to AudioClip and play
        var audioClip = ConvertToAudioClip(audioBroadcast.AudioPacket.AudioData);
        audioSource.PlayOneShot(audioClip, audioBroadcast.VolumeMultiplier);
    }
}
```

### 3D Audio in Unity

```csharp
// Unity 3D positional audio
public class Unity3DAudio : MonoBehaviour
{
    public void UpdatePlayerAudioPosition(string playerId, Vector3 position)
    {
        var audioSource = GetPlayerAudioSource(playerId);
        if (audioSource != null)
        {
            audioSource.transform.position = position;
            
            // Configure 3D audio settings
            audioSource.spatialBlend = 1.0f;        // Full 3D
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 50.0f;
        }
    }
}
```

## 📈 Performance Optimization

### Bandwidth Usage

```
Audio Quality vs Bandwidth:
- Phone Quality (8kHz, 8-bit):  ~8 KB/s per player
- Voice Quality (16kHz, 16-bit): ~32 KB/s per player  
- CD Quality (44kHz, 16-bit):   ~88 KB/s per player

Recommended for games:
- Casual Games: 16kHz, 16-bit, Opus codec (~16 KB/s)
- Competitive Games: 22kHz, 16-bit, Opus codec (~24 KB/s)
```

### CPU Optimization

```csharp
// Efficient audio processing
public class AudioProcessor
{
    private readonly object _processingLock = new object();
    private readonly Queue<AudioPacket> _audioQueue = new Queue<AudioPacket>();
    
    public async Task ProcessAudioAsync()
    {
        await Task.Run(() =>
        {
            while (isProcessing)
            {
                AudioPacket packet = null;
                lock (_processingLock)
                {
                    if (_audioQueue.Count > 0)
                        packet = _audioQueue.Dequeue();
                }
                
                if (packet != null)
                    ProcessAudioPacket(packet);
                
                Thread.Sleep(1); // Prevent CPU spinning
            }
        });
    }
}
```

## 🔒 Security Considerations

### Audio Data Protection

```csharp
// Encrypt audio data for sensitive communications
public class SecureVoIP
{
    public byte[] EncryptAudioData(byte[] audioData, string encryptionKey)
    {
        // Use AES encryption for audio data
        using (var aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32));
            aes.IV = new byte[16]; // Use random IV in production
            
            using (var encryptor = aes.CreateEncryptor())
            {
                return encryptor.TransformFinalBlock(audioData, 0, audioData.Length);
            }
        }
    }
}
```

### Voice Authentication

```csharp
// Verify player identity for voice packets
public class VoiceAuthentication
{
    public bool ValidateVoicePacket(AudioPacket packet, string expectedPlayerId)
    {
        // Verify player ID matches session
        if (packet.PlayerId != expectedPlayerId)
            return false;
        
        // Verify timestamp is recent (prevent replay attacks)
        if (DateTime.UtcNow - packet.Timestamp > TimeSpan.FromSeconds(5))
            return false;
        
        return true;
    }
}
```

## 🎯 Best Practices

1. **Use UDP for audio packets** - Low latency is critical for voice
2. **Implement jitter buffers** - Smooth out network irregularities
3. **Monitor quality metrics** - Adapt to network conditions
4. **Use appropriate codecs** - Opus recommended for most games
5. **Limit transmission range** - Prevent audio spam in large worlds
6. **Implement push-to-talk** - Reduce bandwidth and noise
7. **Cache audio settings** - Persist user preferences
8. **Handle disconnections gracefully** - Clean up audio resources

## 📝 Example Use Cases

### 🏆 **MMO Guild Chat**
```csharp
// Persistent guild voice channels
var guildChannel = voipManager.CreateVoiceChannel($"guild_{guildId}");
guildChannel.IsPersistent = true;
guildChannel.EnableServerMixing = true;  // Mix multiple speakers
```

### ⚡ **FPS Team Communication**
```csharp
// Low-latency competitive voice
var settings = new VoIPSettings
{
    SampleRate = 22050,
    AudioBufferMs = 10,           // Very low latency
    EnableJitterBuffer = false,   // Prefer speed over quality
    ActivationMode = VoiceActivationMode.VoiceActivated
};
```

### 🏁 **Racing Game Proximity Chat**
```csharp
// Speed-based audio range
var distance = CalculateSpeedBasedRange(player.Speed);
settings.MaxAudioDistance = distance;
settings.EnablePositionalAudio = true;
```

---

**Ready to add voice chat to your game?** 🎤

The VoIP system seamlessly extends your existing lobby infrastructure with professional-grade voice communication features! 