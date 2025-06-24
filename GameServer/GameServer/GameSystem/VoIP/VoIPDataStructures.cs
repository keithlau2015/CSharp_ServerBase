using System;
using System.Collections.Generic;
using GameSystem.Lobby;

namespace GameSystem.VoIP
{
    #region VoIP Settings

    public class VoIPSettings
    {
        // Audio Quality
        public int SampleRate { get; set; } = 16000;        // 16kHz for voice (8kHz, 16kHz, 22kHz, 44kHz)
        public int Channels { get; set; } = 1;              // Mono for voice chat
        public int BitsPerSample { get; set; } = 16;        // 16-bit audio
        public int BitRate { get; set; } = 32000;           // 32kbps for compressed voice
        
        // Audio Processing
        public bool EnableNoiseReduction { get; set; } = true;
        public bool EnableAutoGainControl { get; set; } = true;
        public bool EnableEchoCancellation { get; set; } = true;
        public float NoiseGateThreshold { get; set; } = 0.02f;  // 2% threshold
        
        // Positional Audio
        public bool EnablePositionalAudio { get; set; } = true;
        public bool Enable3DAudio { get; set; } = true;
        public float MinAudioDistance { get; set; } = 1.0f;     // Full volume within 1 unit
        public float MaxAudioDistance { get; set; } = 50.0f;    // No audio beyond 50 units
        
        // Voice Activation
        public VoiceActivationMode ActivationMode { get; set; } = VoiceActivationMode.VoiceActivated;
        public float VoiceActivationThreshold { get; set; } = 0.05f;  // 5% threshold
        public int VoiceActivationDelay { get; set; } = 500;    // 500ms delay before stop
        
        // Codec Settings
        public AudioCodec PreferredCodec { get; set; } = AudioCodec.Opus;
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Medium;
        
        // Network Settings
        public int MaxPacketSize { get; set; } = 1200;          // UDP packet size limit
        public int AudioBufferMs { get; set; } = 20;            // 20ms audio buffer
        public bool EnableJitterBuffer { get; set; } = true;
        public int JitterBufferMs { get; set; } = 60;           // 60ms jitter buffer
    }

    public enum VoiceActivationMode
    {
        PushToTalk,
        VoiceActivated,
        OpenMic
    }

    public enum AudioCodec
    {
        PCM,        // Uncompressed
        Opus,       // High quality, low latency
        Speex,      // Good for voice
        AAC,        // High quality
        MP3         // Widely supported
    }

    public enum CompressionLevel
    {
        None,
        Low,
        Medium,
        High,
        Maximum
    }

    #endregion

    #region Voice Channel

    public class VoiceChannel : IDisposable
    {
        public string RoomId { get; }
        public VoIPSettings Settings { get; }
        public DateTime CreatedAt { get; }
        public DateTime LastActivity { get; set; }
        
        // Channel state
        public bool IsActive { get; set; } = true;
        public int ActiveSpeakers { get; set; } = 0;
        public Dictionary<string, DateTime> LastSpeechActivity { get; } = new Dictionary<string, DateTime>();
        
        // Audio mixing (for server-side mixing if needed)
        public bool EnableServerMixing { get; set; } = false;
        public float MasterVolume { get; set; } = 1.0f;
        
        public VoiceChannel(string roomId, VoIPSettings settings)
        {
            RoomId = roomId;
            Settings = settings;
            CreatedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }
        
        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
        
        public void UpdatePlayerSpeechActivity(string playerId)
        {
            LastSpeechActivity[playerId] = DateTime.UtcNow;
            UpdateActivity();
        }
        
        public TimeSpan GetPlayerIdleTime(string playerId)
        {
            if (LastSpeechActivity.TryGetValue(playerId, out var lastActivity))
            {
                return DateTime.UtcNow - lastActivity;
            }
            return TimeSpan.MaxValue;
        }
        
        public void Dispose()
        {
            IsActive = false;
            LastSpeechActivity.Clear();
        }
    }

    #endregion

    #region Player Voice State

    public class PlayerVoiceState
    {
        public string PlayerId { get; }
        public bool IsMuted { get; set; } = false;
        public bool IsDeafened { get; set; } = false;
        public bool IsTalking { get; set; } = false;
        public bool IsPushToTalkActive { get; set; } = false;
        
        // Voice metrics
        public float Volume { get; set; } = 1.0f;
        public float InputVolume { get; set; } = 0.0f;        // Current input level
        public float OutputVolume { get; set; } = 1.0f;       // Volume for this player's voice
        public DateTime LastActivity { get; set; }
        
        // Voice quality
        public float SignalQuality { get; set; } = 1.0f;
        public int PacketLoss { get; set; } = 0;
        public float Latency { get; set; } = 0.0f;
        
        public PlayerVoiceState(string playerId)
        {
            PlayerId = playerId;
            LastActivity = DateTime.UtcNow;
        }
        
        public bool CanSpeak => !IsMuted && IsTalking;
        public bool CanHear => !IsDeafened;
        
        public TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - LastActivity;
        }
    }

    #endregion

    #region Audio Data Structures

    [ProtoBuf.ProtoContract]
    public class AudioPacket
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string PlayerName { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public byte[] AudioData { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public int SampleRate { get; set; } = 16000;
        
        [ProtoBuf.ProtoMember(5)]
        public int Channels { get; set; } = 1;
        
        [ProtoBuf.ProtoMember(6)]
        public int BitsPerSample { get; set; } = 16;
        
        [ProtoBuf.ProtoMember(7)]
        public DateTime Timestamp { get; set; }
        
        [ProtoBuf.ProtoMember(8)]
        public PlayerPosition Position { get; set; }
        
        [ProtoBuf.ProtoMember(9)]
        public float Volume { get; set; } = 1.0f;
        
        [ProtoBuf.ProtoMember(10)]
        public AudioCodec Codec { get; set; } = AudioCodec.PCM;
        
        [ProtoBuf.ProtoMember(11)]
        public int SequenceNumber { get; set; }
        
        [ProtoBuf.ProtoMember(12)]
        public bool IsEndOfSpeech { get; set; } = false;
        
        public int GetDataSizeBytes()
        {
            return AudioData?.Length ?? 0;
        }
        
        public double GetDurationMs()
        {
            if (AudioData == null || AudioData.Length == 0)
                return 0;
                
            int bytesPerSample = BitsPerSample / 8;
            int totalSamples = AudioData.Length / (bytesPerSample * Channels);
            return (double)totalSamples / SampleRate * 1000.0;
        }
    }

    [ProtoBuf.ProtoContract]
    public class AudioBroadcast
    {
        [ProtoBuf.ProtoMember(1)]
        public AudioPacket AudioPacket { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string RoomId { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
        
        [ProtoBuf.ProtoMember(4)]
        public float DistanceFromListener { get; set; } = 0.0f;
        
        [ProtoBuf.ProtoMember(5)]
        public float VolumeMultiplier { get; set; } = 1.0f;
    }

    [ProtoBuf.ProtoContract]
    public class VoiceStateUpdate
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public bool IsMuted { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public bool IsDeafened { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public bool IsTalking { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public float Volume { get; set; } = 1.0f;
        
        [ProtoBuf.ProtoMember(6)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [ProtoBuf.ProtoContract]
    public class VoiceSettingsUpdate
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public VoiceActivationMode ActivationMode { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public float VoiceActivationThreshold { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public bool EnablePositionalAudio { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public float InputVolume { get; set; } = 1.0f;
        
        [ProtoBuf.ProtoMember(6)]
        public float OutputVolume { get; set; } = 1.0f;
        
        [ProtoBuf.ProtoMember(7)]
        public AudioCodec PreferredCodec { get; set; }
    }

    #endregion

    #region Voice Quality Metrics

    [ProtoBuf.ProtoContract]
    public class VoiceQualityMetrics
    {
        [ProtoBuf.ProtoMember(1)]
        public string PlayerId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public float SignalToNoiseRatio { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public float PacketLossPercentage { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public float AverageLatency { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public float JitterMs { get; set; }
        
        [ProtoBuf.ProtoMember(6)]
        public int DroppedPackets { get; set; }
        
        [ProtoBuf.ProtoMember(7)]
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
        
        [ProtoBuf.ProtoMember(8)]
        public string CodecUsed { get; set; }
        
        [ProtoBuf.ProtoMember(9)]
        public int BitRateKbps { get; set; }
    }

    #endregion

    #region Audio Device Info

    [ProtoBuf.ProtoContract]
    public class AudioDeviceInfo
    {
        [ProtoBuf.ProtoMember(1)]
        public string DeviceId { get; set; }
        
        [ProtoBuf.ProtoMember(2)]
        public string DeviceName { get; set; }
        
        [ProtoBuf.ProtoMember(3)]
        public bool IsDefault { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public bool IsInput { get; set; }
        
        [ProtoBuf.ProtoMember(5)]
        public int[] SupportedSampleRates { get; set; }
        
        [ProtoBuf.ProtoMember(6)]
        public int[] SupportedChannels { get; set; }
        
        [ProtoBuf.ProtoMember(7)]
        public bool IsAvailable { get; set; } = true;
    }

    [ProtoBuf.ProtoContract]
    public class AudioDeviceList
    {
        [ProtoBuf.ProtoMember(1)]
        public List<AudioDeviceInfo> InputDevices { get; set; } = new List<AudioDeviceInfo>();
        
        [ProtoBuf.ProtoMember(2)]
        public List<AudioDeviceInfo> OutputDevices { get; set; } = new List<AudioDeviceInfo>();
        
        [ProtoBuf.ProtoMember(3)]
        public string DefaultInputDevice { get; set; }
        
        [ProtoBuf.ProtoMember(4)]
        public string DefaultOutputDevice { get; set; }
    }

    #endregion
} 