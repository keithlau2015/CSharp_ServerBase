using System;
using System.Threading.Tasks;
using GameSystem.Lobby;
using GameSystem.VoIP;

namespace Network
{
    #region Audio Packet Handler (UDP)
    
    public class AudioPacketHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var audioPacket = packet.ReadObject<AudioPacket>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    
                    // Check if player can speak
                    if (!voiceState.CanSpeak)
                        return;
                    
                    // Update voice activity
                    VoIPManager.Instance.SetPlayerTalking(player.UID, player.CurrentRoomId, true);
                    
                    // Process and broadcast audio
                    await VoIPManager.Instance.ProcessAudioPacket(player.UID, player.CurrentRoomId, audioPacket);
                    
                    Debug.DebugUtility.DebugLog($"Audio packet received from {player.Name}: {audioPacket.GetDataSizeBytes()} bytes, {audioPacket.GetDurationMs():F1}ms");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"AudioPacketHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Voice State Handler
    
    public class VoiceStateUpdateHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var voiceStateUpdate = packet.ReadObject<VoiceStateUpdate>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null)
                {
                    // Update voice state
                    VoIPManager.Instance.SetPlayerMuted(player.UID, voiceStateUpdate.IsMuted);
                    VoIPManager.Instance.SetPlayerDeafened(player.UID, voiceStateUpdate.IsDeafened);
                    
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    voiceState.Volume = voiceStateUpdate.Volume;
                    
                    // Broadcast voice state change to room
                    if (!string.IsNullOrEmpty(player.CurrentRoomId))
                    {
                        var broadcastUpdate = new VoiceStateBroadcast
                        {
                            PlayerId = player.UID,
                            PlayerName = player.Name,
                            IsMuted = voiceStateUpdate.IsMuted,
                            IsDeafened = voiceStateUpdate.IsDeafened,
                            IsTalking = voiceStateUpdate.IsTalking,
                            Volume = voiceStateUpdate.Volume,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        LobbyManager.Instance.BroadcastToRoom(
                            player.CurrentRoomId,
                            "VoiceStateBroadcast",
                            broadcastUpdate,
                            excludePlayer: player.UID
                        );
                    }
                    
                    Debug.DebugUtility.DebugLog($"Voice state updated for {player.Name}: Muted={voiceStateUpdate.IsMuted}, Deafened={voiceStateUpdate.IsDeafened}");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"VoiceStateUpdateHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Voice Settings Handler
    
    public class VoiceSettingsUpdateHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var settingsUpdate = packet.ReadObject<VoiceSettingsUpdate>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null)
                {
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    voiceState.InputVolume = settingsUpdate.InputVolume;
                    voiceState.OutputVolume = settingsUpdate.OutputVolume;
                    
                    Debug.DebugUtility.DebugLog($"Voice settings updated for {player.Name}: InputVol={settingsUpdate.InputVolume:F2}, OutputVol={settingsUpdate.OutputVolume:F2}");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"VoiceSettingsUpdateHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Push-to-Talk Handler
    
    public class PushToTalkHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var pttState = packet.ReadObject<PushToTalkState>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null && !string.IsNullOrEmpty(player.CurrentRoomId))
                {
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    voiceState.IsPushToTalkActive = pttState.IsActive;
                    
                    // Update talking state based on PTT and mute status
                    bool shouldTalk = pttState.IsActive && !voiceState.IsMuted;
                    VoIPManager.Instance.SetPlayerTalking(player.UID, player.CurrentRoomId, shouldTalk);
                    
                    // Broadcast PTT state to room
                    var broadcastState = new PushToTalkBroadcast
                    {
                        PlayerId = player.UID,
                        PlayerName = player.Name,
                        IsActive = pttState.IsActive,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    LobbyManager.Instance.BroadcastToRoomUDP(
                        player.CurrentRoomId,
                        "PushToTalkBroadcast",
                        broadcastState,
                        excludePlayer: player.UID
                    );
                    
                    Debug.DebugUtility.DebugLog($"Push-to-talk {(pttState.IsActive ? "activated" : "deactivated")} for {player.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"PushToTalkHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Voice Quality Metrics Handler
    
    public class VoiceQualityMetricsHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                var metrics = packet.ReadObject<VoiceQualityMetrics>();
                
                var player = LobbyManager.Instance.GetPlayer(netClient.UID.ToString());
                if (player != null)
                {
                    var voiceState = VoIPManager.Instance.GetPlayerVoiceState(player.UID);
                    voiceState.SignalQuality = metrics.SignalToNoiseRatio;
                    voiceState.PacketLoss = (int)metrics.PacketLossPercentage;
                    voiceState.Latency = metrics.AverageLatency;
                    
                    // Log quality issues
                    if (metrics.PacketLossPercentage > 5.0f)
                    {
                        Debug.DebugUtility.WarningLog($"High packet loss for {player.Name}: {metrics.PacketLossPercentage:F1}%");
                    }
                    
                    if (metrics.AverageLatency > 150.0f)
                    {
                        Debug.DebugUtility.WarningLog($"High latency for {player.Name}: {metrics.AverageLatency:F1}ms");
                    }
                    
                    Debug.DebugUtility.DebugLog($"Voice quality metrics for {player.Name}: Loss={metrics.PacketLossPercentage:F1}%, Latency={metrics.AverageLatency:F1}ms");
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"VoiceQualityMetricsHandler error: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Audio Device Handler
    
    public class AudioDeviceRequestHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            try
            {
                // Client is requesting available audio devices
                var deviceList = GetAvailableAudioDevices();
                
                var response = new AudioDeviceListResponse
                {
                    DeviceList = deviceList,
                    Success = true,
                    Message = "Audio devices retrieved successfully"
                };
                
                var responsePacket = new Packet("AudioDeviceListResponse");
                responsePacket.Write(response);
                netClient.Send(responsePacket);
                
                Debug.DebugUtility.DebugLog($"Audio device list sent to {netClient.UID}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"AudioDeviceRequestHandler error: {ex.Message}");
                
                var errorResponse = new AudioDeviceListResponse
                {
                    Success = false,
                    Message = $"Failed to get audio devices: {ex.Message}"
                };
                
                var responsePacket = new Packet("AudioDeviceListResponse");
                responsePacket.Write(errorResponse);
                netClient.Send(responsePacket);
            }
        }
        
        private AudioDeviceList GetAvailableAudioDevices()
        {
            // In a real implementation, this would query the system for available audio devices
            // For demo purposes, return some mock devices
            return new AudioDeviceList
            {
                InputDevices = new System.Collections.Generic.List<AudioDeviceInfo>
                {
                    new AudioDeviceInfo
                    {
                        DeviceId = "input_1",
                        DeviceName = "Default Microphone",
                        IsDefault = true,
                        IsInput = true,
                        SupportedSampleRates = new[] { 8000, 16000, 22050, 44100, 48000 },
                        SupportedChannels = new[] { 1, 2 },
                        IsAvailable = true
                    }
                },
                OutputDevices = new System.Collections.Generic.List<AudioDeviceInfo>
                {
                    new AudioDeviceInfo
                    {
                        DeviceId = "output_1",
                        DeviceName = "Default Speakers",
                        IsDefault = true,
                        IsInput = false,
                        SupportedSampleRates = new[] { 8000, 16000, 22050, 44100, 48000 },
                        SupportedChannels = new[] { 1, 2 },
                        IsAvailable = true
                    }
                },
                DefaultInputDevice = "input_1",
                DefaultOutputDevice = "output_1"
            };
        }
    }
    
    #endregion
}

#region Data Transfer Objects

[ProtoBuf.ProtoContract]
public class VoiceStateBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public bool IsMuted { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public bool IsDeafened { get; set; }
    
    [ProtoBuf.ProtoMember(5)]
    public bool IsTalking { get; set; }
    
    [ProtoBuf.ProtoMember(6)]
    public float Volume { get; set; }
    
    [ProtoBuf.ProtoMember(7)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class PushToTalkState
{
    [ProtoBuf.ProtoMember(1)]
    public bool IsActive { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

[ProtoBuf.ProtoContract]
public class PushToTalkBroadcast
{
    [ProtoBuf.ProtoMember(1)]
    public string PlayerId { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string PlayerName { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public bool IsActive { get; set; }
    
    [ProtoBuf.ProtoMember(4)]
    public DateTime Timestamp { get; set; }
}

[ProtoBuf.ProtoContract]
public class AudioDeviceListResponse
{
    [ProtoBuf.ProtoMember(1)]
    public bool Success { get; set; }
    
    [ProtoBuf.ProtoMember(2)]
    public string Message { get; set; }
    
    [ProtoBuf.ProtoMember(3)]
    public GameSystem.VoIP.AudioDeviceList DeviceList { get; set; }
}

#endregion 