using System;
using System.Threading.Tasks;
using System.Threading;
using GameSystem.VoIP;
using GameSystem.Lobby;
using Network;

namespace Demo
{
    /// <summary>
    /// Example VoIP client implementation showing how to integrate voice chat
    /// with the existing lobby and room system
    /// </summary>
    public class VoIPClientExample
    {
        private NetClient netClient;
        private bool isConnected = false;
        private string playerId;
        private string currentRoomId;
        
        // VoIP State
        private bool isMicrophoneActive = false;
        private bool isMuted = false;
        private bool isDeafened = false;
        private bool isPushToTalkActive = false;
        private VoiceActivationMode activationMode = VoiceActivationMode.VoiceActivated;
        
        // Audio simulation (in real implementation, these would interface with audio hardware)
        private Timer audioTimer;
        private Random random = new Random();
        private int audioSequenceNumber = 0;
        
        // Settings
        private VoIPSettings voipSettings = new VoIPSettings();
        
        public event Action<AudioBroadcast> OnAudioReceived;
        public event Action<VoiceStateBroadcast> OnPlayerVoiceStateChanged;
        public event Action<PushToTalkBroadcast> OnPlayerPushToTalkChanged;
        
        #region Connection Management
        
        public async Task ConnectToServer(string serverIP, int port)
        {
            try
            {
                netClient = new NetClient();
                isConnected = true;
                playerId = netClient.UID.ToString();
                
                Console.WriteLine($"🔗 VoIP Client connected as {playerId}");
                
                // Request available audio devices
                await RequestAudioDevices();
                
                // Initialize audio simulation
                InitializeAudioSimulation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to connect: {ex.Message}");
            }
        }
        
        public void Disconnect()
        {
            if (isConnected)
            {
                StopVoiceTransmission();
                audioTimer?.Dispose();
                netClient?.Disconnect();
                isConnected = false;
                Console.WriteLine("🔌 VoIP Client disconnected");
            }
        }
        
        #endregion
        
        #region Room Operations
        
        public async Task JoinRoom(string roomId, string playerName)
        {
            if (!isConnected) return;
            
            currentRoomId = roomId;
            Console.WriteLine($"🏠 Joined room {roomId} - VoIP enabled");
            
            // Send initial voice state
            await SendVoiceStateUpdate();
            
            Console.WriteLine("🎤 Voice chat is now active in this room");
            Console.WriteLine("Commands:");
            Console.WriteLine("  - Press 'M' to toggle mute");
            Console.WriteLine("  - Press 'D' to toggle deafen");
            Console.WriteLine("  - Press 'T' for push-to-talk");
            Console.WriteLine("  - Press 'V' to start/stop voice transmission");
        }
        
        public async Task LeaveRoom()
        {
            if (!string.IsNullOrEmpty(currentRoomId))
            {
                await StopVoiceTransmission();
                currentRoomId = null;
                Console.WriteLine("🏠 Left room - VoIP disabled");
            }
        }
        
        #endregion
        
        #region Voice Control
        
        public async Task ToggleMute()
        {
            isMuted = !isMuted;
            await SendVoiceStateUpdate();
            Console.WriteLine($"🔇 Microphone {(isMuted ? "MUTED" : "UNMUTED")}");
        }
        
        public async Task ToggleDeafen()
        {
            isDeafened = !isDeafened;
            await SendVoiceStateUpdate();
            Console.WriteLine($"🔊 Audio {(isDeafened ? "DEAFENED" : "ENABLED")}");
        }
        
        public async Task StartPushToTalk()
        {
            if (activationMode == VoiceActivationMode.PushToTalk)
            {
                isPushToTalkActive = true;
                await SendPushToTalkState();
                await StartVoiceTransmission();
                Console.WriteLine("🎤 Push-to-talk ACTIVATED");
            }
        }
        
        public async Task StopPushToTalk()
        {
            if (activationMode == VoiceActivationMode.PushToTalk)
            {
                isPushToTalkActive = false;
                await SendPushToTalkState();
                await StopVoiceTransmission();
                Console.WriteLine("🎤 Push-to-talk DEACTIVATED");
            }
        }
        
        public async Task StartVoiceTransmission()
        {
            if (string.IsNullOrEmpty(currentRoomId) || isMuted) return;
            
            if (!isMicrophoneActive)
            {
                isMicrophoneActive = true;
                audioTimer?.Dispose();
                
                // Start audio transmission timer (simulating 20ms audio chunks)
                audioTimer = new Timer(SendAudioChunk, null, 0, voipSettings.AudioBufferMs);
                
                Console.WriteLine("🎤 Voice transmission STARTED");
            }
        }
        
        public async Task StopVoiceTransmission()
        {
            if (isMicrophoneActive)
            {
                isMicrophoneActive = false;
                audioTimer?.Dispose();
                
                // Send end-of-speech packet
                await SendEndOfSpeechPacket();
                
                Console.WriteLine("🎤 Voice transmission STOPPED");
            }
        }
        
        #endregion
        
        #region Audio Simulation
        
        private void InitializeAudioSimulation()
        {
            // In a real implementation, this would initialize audio capture/playback hardware
            Console.WriteLine("🔊 Audio system initialized (simulated)");
            Console.WriteLine($"   Sample Rate: {voipSettings.SampleRate}Hz");
            Console.WriteLine($"   Channels: {voipSettings.Channels}");
            Console.WriteLine($"   Buffer Size: {voipSettings.AudioBufferMs}ms");
        }
        
        private async void SendAudioChunk(object state)
        {
            if (!isMicrophoneActive || string.IsNullOrEmpty(currentRoomId) || isMuted)
                return;
            
            // Generate simulated audio data (in real implementation, capture from microphone)
            var audioData = GenerateSimulatedAudioData();
            
            var audioPacket = new AudioPacket
            {
                PlayerId = playerId,
                PlayerName = "VoIPClient",
                AudioData = audioData,
                SampleRate = voipSettings.SampleRate,
                Channels = voipSettings.Channels,
                BitsPerSample = voipSettings.BitsPerSample,
                Timestamp = DateTime.UtcNow,
                Position = new PlayerPosition { X = 0, Y = 0, Z = 0 }, // Current player position
                Volume = 1.0f,
                Codec = voipSettings.PreferredCodec,
                SequenceNumber = ++audioSequenceNumber,
                IsEndOfSpeech = false
            };
            
            // Send via UDP for low latency
            var packet = new Packet("AudioPacket");
            packet.Write(audioPacket);
            netClient?.Send(packet, NetClient.SupportProtocol.UDP);
            
            // Simulate voice quality metrics occasionally
            if (audioSequenceNumber % 100 == 0) // Every 2 seconds at 50Hz
            {
                await SendVoiceQualityMetrics();
            }
        }
        
        private byte[] GenerateSimulatedAudioData()
        {
            // Generate simulated audio data for demo purposes
            int samplesPerBuffer = (voipSettings.SampleRate * voipSettings.AudioBufferMs) / 1000;
            int bytesPerSample = voipSettings.BitsPerSample / 8;
            int totalBytes = samplesPerBuffer * voipSettings.Channels * bytesPerSample;
            
            var audioData = new byte[totalBytes];
            
            // Generate simple sine wave for demonstration
            for (int i = 0; i < samplesPerBuffer; i++)
            {
                double time = (double)i / voipSettings.SampleRate;
                double frequency = 440.0; // A4 note
                double amplitude = 0.3; // 30% volume
                
                short sample = (short)(amplitude * Math.Sin(2 * Math.PI * frequency * time) * 32767);
                
                // Add some random noise for realism
                sample += (short)(random.Next(-1000, 1000));
                
                // Write sample (16-bit, little endian)
                audioData[i * 2] = (byte)(sample & 0xFF);
                audioData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            
            return audioData;
        }
        
        #endregion
        
        #region Network Communication
        
        private async Task SendVoiceStateUpdate()
        {
            if (!isConnected) return;
            
            var voiceState = new VoiceStateUpdate
            {
                PlayerId = playerId,
                IsMuted = isMuted,
                IsDeafened = isDeafened,
                IsTalking = isMicrophoneActive && !isMuted,
                Volume = 1.0f,
                Timestamp = DateTime.UtcNow
            };
            
            var packet = new Packet("VoiceStateUpdate");
            packet.Write(voiceState);
            netClient?.Send(packet);
        }
        
        private async Task SendPushToTalkState()
        {
            if (!isConnected) return;
            
            var pttState = new PushToTalkState
            {
                IsActive = isPushToTalkActive,
                Timestamp = DateTime.UtcNow
            };
            
            var packet = new Packet("PushToTalkState");
            packet.Write(pttState);
            netClient?.Send(packet, NetClient.SupportProtocol.UDP);
        }
        
        private async Task SendVoiceQualityMetrics()
        {
            if (!isConnected) return;
            
            var metrics = new VoiceQualityMetrics
            {
                PlayerId = playerId,
                SignalToNoiseRatio = 15.0f + (float)(random.NextDouble() * 10.0), // 15-25 dB
                PacketLossPercentage = (float)(random.NextDouble() * 2.0), // 0-2%
                AverageLatency = 50.0f + (float)(random.NextDouble() * 50.0), // 50-100ms
                JitterMs = (float)(random.NextDouble() * 10.0), // 0-10ms
                DroppedPackets = random.Next(0, 3),
                CodecUsed = voipSettings.PreferredCodec.ToString(),
                BitRateKbps = voipSettings.BitRate / 1000,
                MeasuredAt = DateTime.UtcNow
            };
            
            var packet = new Packet("VoiceQualityMetrics");
            packet.Write(metrics);
            netClient?.Send(packet);
        }
        
        private async Task SendEndOfSpeechPacket()
        {
            if (!isConnected) return;
            
            var endPacket = new AudioPacket
            {
                PlayerId = playerId,
                PlayerName = "VoIPClient",
                AudioData = new byte[0],
                SampleRate = voipSettings.SampleRate,
                Channels = voipSettings.Channels,
                BitsPerSample = voipSettings.BitsPerSample,
                Timestamp = DateTime.UtcNow,
                Position = new PlayerPosition { X = 0, Y = 0, Z = 0 },
                Volume = 0.0f,
                Codec = voipSettings.PreferredCodec,
                SequenceNumber = ++audioSequenceNumber,
                IsEndOfSpeech = true
            };
            
            var packet = new Packet("AudioPacket");
            packet.Write(endPacket);
            netClient?.Send(packet, NetClient.SupportProtocol.UDP);
        }
        
        private async Task RequestAudioDevices()
        {
            if (!isConnected) return;
            
            var packet = new Packet("AudioDeviceRequest");
            netClient?.Send(packet);
        }
        
        #endregion
        
        #region Settings Management
        
        public void UpdateVoIPSettings(VoIPSettings newSettings)
        {
            voipSettings = newSettings;
            
            // Send settings update to server
            var settingsUpdate = new VoiceSettingsUpdate
            {
                PlayerId = playerId,
                ActivationMode = newSettings.ActivationMode,
                VoiceActivationThreshold = newSettings.VoiceActivationThreshold,
                EnablePositionalAudio = newSettings.EnablePositionalAudio,
                InputVolume = 1.0f,
                OutputVolume = 1.0f,
                PreferredCodec = newSettings.PreferredCodec
            };
            
            var packet = new Packet("VoiceSettingsUpdate");
            packet.Write(settingsUpdate);
            netClient?.Send(packet);
            
            Console.WriteLine("🔧 VoIP settings updated");
        }
        
        public void SetActivationMode(VoiceActivationMode mode)
        {
            activationMode = mode;
            voipSettings.ActivationMode = mode;
            
            Console.WriteLine($"🎤 Voice activation mode: {mode}");
            
            if (mode == VoiceActivationMode.OpenMic)
            {
                Task.Run(async () => await StartVoiceTransmission());
            }
            else if (mode == VoiceActivationMode.VoiceActivated)
            {
                Console.WriteLine("🎤 Voice activation enabled - speak to transmit");
            }
            else if (mode == VoiceActivationMode.PushToTalk)
            {
                Console.WriteLine("🎤 Push-to-talk enabled - press 'T' to talk");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        public void HandleAudioReceived(AudioBroadcast audioBroadcast)
        {
            if (isDeafened) return;
            
            var audio = audioBroadcast.AudioPacket;
            
            // In a real implementation, this would play the audio through speakers
            if (!audio.IsEndOfSpeech)
            {
                Console.WriteLine($"🔊 Receiving audio from {audio.PlayerName}: {audio.GetDataSizeBytes()} bytes, Volume: {audio.Volume:F2}");
                
                // Simulate positional audio
                if (voipSettings.EnablePositionalAudio && audioBroadcast.DistanceFromListener > 0)
                {
                    Console.WriteLine($"   📍 Distance: {audioBroadcast.DistanceFromListener:F1} units, 3D Volume: {audioBroadcast.VolumeMultiplier:F2}");
                }
            }
            else
            {
                Console.WriteLine($"🔇 {audio.PlayerName} stopped speaking");
            }
            
            OnAudioReceived?.Invoke(audioBroadcast);
        }
        
        public void HandlePlayerVoiceStateChanged(VoiceStateBroadcast voiceState)
        {
            Console.WriteLine($"👤 {voiceState.PlayerName} voice state: Muted={voiceState.IsMuted}, Talking={voiceState.IsTalking}");
            OnPlayerVoiceStateChanged?.Invoke(voiceState);
        }
        
        public void HandlePlayerPushToTalkChanged(PushToTalkBroadcast pttState)
        {
            Console.WriteLine($"🎤 {pttState.PlayerName} push-to-talk: {(pttState.IsActive ? "ACTIVE" : "INACTIVE")}");
            OnPlayerPushToTalkChanged?.Invoke(pttState);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Demo application showing VoIP integration with the lobby system
    /// </summary>
    public class VoIPDemo
    {
        public static async Task RunVoIPDemo()
        {
            Console.WriteLine("🎤 VoIP Integration Demo");
            Console.WriteLine("========================");
            
            var voipClient = new VoIPClientExample();
            
            try
            {
                // Connect to server
                await voipClient.ConnectToServer("127.0.0.1", 8080);
                
                // Join a room
                await voipClient.JoinRoom("demo-room", "VoIPTester");
                
                // Configure VoIP settings
                var settings = new VoIPSettings
                {
                    SampleRate = 16000,
                    Channels = 1,
                    BitsPerSample = 16,
                    EnablePositionalAudio = true,
                    ActivationMode = VoiceActivationMode.VoiceActivated,
                    EnableNoiseReduction = true,
                    EnableAutoGainControl = true
                };
                
                voipClient.UpdateVoIPSettings(settings);
                voipClient.SetActivationMode(VoiceActivationMode.VoiceActivated);
                
                // Simulate voice chat for 10 seconds
                Console.WriteLine("🎤 Starting voice transmission simulation...");
                await voipClient.StartVoiceTransmission();
                
                await Task.Delay(5000); // Transmit for 5 seconds
                
                await voipClient.StopVoiceTransmission();
                
                // Test mute/unmute
                Console.WriteLine("🔇 Testing mute functionality...");
                await voipClient.ToggleMute();
                await Task.Delay(1000);
                await voipClient.ToggleMute();
                
                // Test push-to-talk
                Console.WriteLine("🎤 Testing push-to-talk...");
                voipClient.SetActivationMode(VoiceActivationMode.PushToTalk);
                await voipClient.StartPushToTalk();
                await Task.Delay(2000);
                await voipClient.StopPushToTalk();
                
                Console.WriteLine("✅ VoIP demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ VoIP demo failed: {ex.Message}");
            }
            finally
            {
                voipClient.Disconnect();
            }
        }
    }
} 