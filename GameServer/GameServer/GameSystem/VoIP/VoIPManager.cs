using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameSystem.Lobby;
using Network;

namespace GameSystem.VoIP
{
    public class VoIPManager
    {
        private static VoIPManager _instance;
        public static VoIPManager Instance => _instance ??= new VoIPManager();

        // Audio settings
        public VoIPSettings Settings { get; set; } = new VoIPSettings();
        
        // Active voice channels per room
        private readonly ConcurrentDictionary<string, VoiceChannel> _voiceChannels = new ConcurrentDictionary<string, VoiceChannel>();
        
        // Player voice states
        private readonly ConcurrentDictionary<string, PlayerVoiceState> _playerVoiceStates = new ConcurrentDictionary<string, PlayerVoiceState>();
        
        // Events
        public event Action<string, string> OnPlayerStartedTalking;  // roomId, playerId
        public event Action<string, string> OnPlayerStoppedTalking;  // roomId, playerId
        public event Action<string, AudioPacket> OnAudioReceived;    // playerId, audioData
        
        private VoIPManager() { }

        #region Voice Channel Management

        public VoiceChannel CreateVoiceChannel(string roomId)
        {
            var channel = new VoiceChannel(roomId, Settings);
            
            if (_voiceChannels.TryAdd(roomId, channel))
            {
                Debug.DebugUtility.DebugLog($"Voice channel created for room: {roomId}");
                return channel;
            }
            
            return _voiceChannels.GetValueOrDefault(roomId);
        }

        public void DestroyVoiceChannel(string roomId)
        {
            if (_voiceChannels.TryRemove(roomId, out var channel))
            {
                channel.Dispose();
                Debug.DebugUtility.DebugLog($"Voice channel destroyed for room: {roomId}");
            }
        }

        public VoiceChannel GetVoiceChannel(string roomId)
        {
            return _voiceChannels.GetValueOrDefault(roomId);
        }

        #endregion

        #region Player Voice Management

        public PlayerVoiceState GetPlayerVoiceState(string playerId)
        {
            return _playerVoiceStates.GetOrAdd(playerId, id => new PlayerVoiceState(id));
        }

        public void SetPlayerMuted(string playerId, bool muted)
        {
            var voiceState = GetPlayerVoiceState(playerId);
            voiceState.IsMuted = muted;
            
            Debug.DebugUtility.DebugLog($"Player {playerId} muted: {muted}");
        }

        public void SetPlayerDeafened(string playerId, bool deafened)
        {
            var voiceState = GetPlayerVoiceState(playerId);
            voiceState.IsDeafened = deafened;
            
            Debug.DebugUtility.DebugLog($"Player {playerId} deafened: {deafened}");
        }

        public void SetPlayerTalking(string playerId, string roomId, bool talking)
        {
            var voiceState = GetPlayerVoiceState(playerId);
            bool wasAlreadyTalking = voiceState.IsTalking;
            voiceState.IsTalking = talking;
            voiceState.LastActivity = DateTime.UtcNow;

            if (talking && !wasAlreadyTalking)
            {
                OnPlayerStartedTalking?.Invoke(roomId, playerId);
                Debug.DebugUtility.DebugLog($"Player {playerId} started talking in room {roomId}");
            }
            else if (!talking && wasAlreadyTalking)
            {
                OnPlayerStoppedTalking?.Invoke(roomId, playerId);
                Debug.DebugUtility.DebugLog($"Player {playerId} stopped talking in room {roomId}");
            }
        }

        #endregion

        #region Audio Processing

        public async Task ProcessAudioPacket(string senderId, string roomId, AudioPacket audioPacket)
        {
            var senderVoiceState = GetPlayerVoiceState(senderId);
            
            // Check if sender is muted
            if (senderVoiceState.IsMuted)
                return;

            var channel = GetVoiceChannel(roomId);
            if (channel == null)
                return;

            // Get room and players
            var room = LobbyManager.Instance.GetRoom(roomId);
            if (room == null)
                return;

            var senderPlayer = LobbyManager.Instance.GetPlayer(senderId);
            if (senderPlayer == null)
                return;

            // Process audio based on settings
            var processedAudio = await ProcessAudioData(audioPacket, senderPlayer, room);
            
            // Broadcast to other players in the room
            await BroadcastAudio(roomId, senderId, processedAudio, senderPlayer.Position);
        }

        private async Task<AudioPacket> ProcessAudioData(AudioPacket audioPacket, Player sender, GameRoom room)
        {
            var processedPacket = new AudioPacket
            {
                PlayerId = sender.UID,
                PlayerName = sender.Name,
                AudioData = audioPacket.AudioData,
                SampleRate = audioPacket.SampleRate,
                Channels = audioPacket.Channels,
                BitsPerSample = audioPacket.BitsPerSample,
                Timestamp = DateTime.UtcNow,
                Position = sender.Position,
                Volume = CalculateVolume(audioPacket),
                Codec = audioPacket.Codec
            };

            // Apply audio processing if needed
            if (Settings.EnableNoiseReduction)
            {
                processedPacket.AudioData = ApplyNoiseReduction(processedPacket.AudioData);
            }

            if (Settings.EnableAutoGainControl)
            {
                processedPacket.AudioData = ApplyAutoGainControl(processedPacket.AudioData);
            }

            return processedPacket;
        }

        private async Task BroadcastAudio(string roomId, string senderId, AudioPacket audioPacket, PlayerPosition senderPosition)
        {
            var room = LobbyManager.Instance.GetRoom(roomId);
            if (room == null) return;

            var players = room.GetPlayers();
            
            foreach (var player in players)
            {
                // Don't send audio back to sender
                if (player.UID == senderId)
                    continue;

                var playerVoiceState = GetPlayerVoiceState(player.UID);
                
                // Skip if player is deafened
                if (playerVoiceState.IsDeafened)
                    continue;

                // Calculate positional audio
                var processedAudio = CalculatePositionalAudio(audioPacket, senderPosition, player.Position);
                
                // Send via UDP for low latency
                var broadcastPacket = new AudioBroadcast
                {
                    AudioPacket = processedAudio,
                    RoomId = roomId
                };

                var packet = new Packet("AudioBroadcast");
                packet.Write(broadcastPacket);
                player.NetClient?.Send(packet, NetClient.SupportProtocol.UDP);
            }
        }

        #endregion

        #region Audio Calculations

        private AudioPacket CalculatePositionalAudio(AudioPacket originalAudio, PlayerPosition senderPos, PlayerPosition listenerPos)
        {
            if (!Settings.EnablePositionalAudio)
                return originalAudio;

            var processedAudio = new AudioPacket
            {
                PlayerId = originalAudio.PlayerId,
                PlayerName = originalAudio.PlayerName,
                AudioData = originalAudio.AudioData,
                SampleRate = originalAudio.SampleRate,
                Channels = originalAudio.Channels,
                BitsPerSample = originalAudio.BitsPerSample,
                Timestamp = originalAudio.Timestamp,
                Position = originalAudio.Position,
                Codec = originalAudio.Codec
            };

            // Calculate distance
            float distance = CalculateDistance(senderPos, listenerPos);
            
            // Apply distance-based volume attenuation
            float volumeMultiplier = CalculateVolumeAttenuation(distance);
            processedAudio.Volume = originalAudio.Volume * volumeMultiplier;
            
            // Apply 3D positioning (stereo panning)
            if (Settings.Enable3DAudio && originalAudio.Channels >= 2)
            {
                var panValue = CalculateStereoPan(senderPos, listenerPos);
                processedAudio.AudioData = ApplyStereoPan(processedAudio.AudioData, panValue);
            }

            return processedAudio;
        }

        private float CalculateDistance(PlayerPosition pos1, PlayerPosition pos2)
        {
            float dx = pos1.X - pos2.X;
            float dy = pos1.Y - pos2.Y;
            float dz = pos1.Z - pos2.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private float CalculateVolumeAttenuation(float distance)
        {
            if (distance <= Settings.MinAudioDistance)
                return 1.0f;
            
            if (distance >= Settings.MaxAudioDistance)
                return 0.0f;

            // Linear attenuation (can be changed to logarithmic)
            float attenuation = 1.0f - ((distance - Settings.MinAudioDistance) / 
                                      (Settings.MaxAudioDistance - Settings.MinAudioDistance));
            
            return Math.Max(0.0f, Math.Min(1.0f, attenuation));
        }

        private float CalculateStereoPan(PlayerPosition senderPos, PlayerPosition listenerPos)
        {
            // Calculate relative position for stereo panning
            float deltaX = senderPos.X - listenerPos.X;
            float deltaZ = senderPos.Z - listenerPos.Z;
            
            // Use atan2 to get angle
            float angle = (float)Math.Atan2(deltaX, deltaZ);
            
            // Convert to pan value (-1 = left, 0 = center, 1 = right)
            return (float)Math.Sin(angle);
        }

        #endregion

        #region Audio Processing Utilities

        private byte[] ApplyNoiseReduction(byte[] audioData)
        {
            // Simple noise gate implementation
            // In production, use a proper noise reduction algorithm
            var threshold = Settings.NoiseGateThreshold;
            var processedData = new byte[audioData.Length];
            
            for (int i = 0; i < audioData.Length; i += 2) // Assuming 16-bit samples
            {
                if (i + 1 < audioData.Length)
                {
                    short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
                    float normalizedSample = sample / 32768.0f;
                    
                    if (Math.Abs(normalizedSample) < threshold)
                    {
                        processedData[i] = 0;
                        processedData[i + 1] = 0;
                    }
                    else
                    {
                        processedData[i] = audioData[i];
                        processedData[i + 1] = audioData[i + 1];
                    }
                }
            }
            
            return processedData;
        }

        private byte[] ApplyAutoGainControl(byte[] audioData)
        {
            // Simple AGC implementation
            // Analyze peak and apply gain adjustment
            float maxSample = 0;
            
            for (int i = 0; i < audioData.Length; i += 2)
            {
                if (i + 1 < audioData.Length)
                {
                    short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
                    float normalizedSample = Math.Abs(sample / 32768.0f);
                    maxSample = Math.Max(maxSample, normalizedSample);
                }
            }
            
            if (maxSample > 0.1f) // Only apply if signal is strong enough
            {
                float gain = Math.Min(0.8f / maxSample, 2.0f); // Limit gain to 2x
                return ApplyGain(audioData, gain);
            }
            
            return audioData;
        }

        private byte[] ApplyGain(byte[] audioData, float gain)
        {
            var processedData = new byte[audioData.Length];
            
            for (int i = 0; i < audioData.Length; i += 2)
            {
                if (i + 1 < audioData.Length)
                {
                    short sample = (short)(audioData[i] | (audioData[i + 1] << 8));
                    float amplifiedSample = (sample / 32768.0f) * gain;
                    
                    // Clamp to prevent clipping
                    amplifiedSample = Math.Max(-1.0f, Math.Min(1.0f, amplifiedSample));
                    
                    short newSample = (short)(amplifiedSample * 32767);
                    processedData[i] = (byte)(newSample & 0xFF);
                    processedData[i + 1] = (byte)((newSample >> 8) & 0xFF);
                }
            }
            
            return processedData;
        }

        private byte[] ApplyStereoPan(byte[] audioData, float panValue)
        {
            if (audioData.Length < 4) return audioData; // Need at least one stereo sample
            
            var processedData = new byte[audioData.Length];
            float leftGain = (1.0f - panValue) * 0.5f + 0.5f;
            float rightGain = (1.0f + panValue) * 0.5f + 0.5f;
            
            for (int i = 0; i < audioData.Length; i += 4) // Stereo samples (2 channels, 2 bytes each)
            {
                if (i + 3 < audioData.Length)
                {
                    // Left channel
                    short leftSample = (short)(audioData[i] | (audioData[i + 1] << 8));
                    leftSample = (short)(leftSample * leftGain);
                    processedData[i] = (byte)(leftSample & 0xFF);
                    processedData[i + 1] = (byte)((leftSample >> 8) & 0xFF);
                    
                    // Right channel
                    short rightSample = (short)(audioData[i + 2] | (audioData[i + 3] << 8));
                    rightSample = (short)(rightSample * rightGain);
                    processedData[i + 2] = (byte)(rightSample & 0xFF);
                    processedData[i + 3] = (byte)((rightSample >> 8) & 0xFF);
                }
            }
            
            return processedData;
        }

        private float CalculateVolume(AudioPacket audioPacket)
        {
            if (audioPacket.AudioData == null || audioPacket.AudioData.Length == 0)
                return 0.0f;

            float sum = 0;
            int sampleCount = 0;
            
            for (int i = 0; i < audioPacket.AudioData.Length; i += 2)
            {
                if (i + 1 < audioPacket.AudioData.Length)
                {
                    short sample = (short)(audioPacket.AudioData[i] | (audioPacket.AudioData[i + 1] << 8));
                    sum += Math.Abs(sample / 32768.0f);
                    sampleCount++;
                }
            }
            
            return sampleCount > 0 ? sum / sampleCount : 0.0f;
        }

        #endregion

        #region Cleanup

        public void RemovePlayer(string playerId)
        {
            _playerVoiceStates.TryRemove(playerId, out _);
            Debug.DebugUtility.DebugLog($"Removed player voice state: {playerId}");
        }

        public void Dispose()
        {
            foreach (var channel in _voiceChannels.Values)
            {
                channel.Dispose();
            }
            _voiceChannels.Clear();
            _playerVoiceStates.Clear();
        }

        #endregion
    }
} 