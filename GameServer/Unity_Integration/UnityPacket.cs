using System;
using System.Text;
using Newtonsoft.Json;

namespace UnityClient
{
    [System.Serializable]
    public class UnityPacket
    {
        public string PacketType { get; set; }
        public string PlayerUID { get; set; }
        public object Data { get; set; }
        
        public UnityPacket() { }
        
        public UnityPacket(string packetType, string playerUID, object data)
        {
            PacketType = packetType;
            PlayerUID = playerUID;
            Data = data;
        }
        
        /// <summary>
        /// Convert packet to bytes for network transmission
        /// </summary>
        public byte[] ToBytes()
        {
            try
            {
                // Create packet data structure
                var packetData = new
                {
                    PacketType = this.PacketType,
                    PlayerUID = this.PlayerUID,
                    Data = this.Data
                };
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(packetData, Formatting.None);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                
                // Create final packet with length prefix (compatible with C# server)
                byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
                byte[] packet = new byte[4 + jsonBytes.Length];
                
                // Copy length first (4 bytes)
                Array.Copy(lengthBytes, 0, packet, 0, 4);
                // Copy JSON data
                Array.Copy(jsonBytes, 0, packet, 4, jsonBytes.Length);
                
                return packet;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to serialize packet '{PacketType}': {ex.Message}");
                return new byte[0];
            }
        }
        
        /// <summary>
        /// Create packet from received bytes
        /// </summary>
        public static UnityPacket FromBytes(byte[] data)
        {
            try
            {
                // Convert bytes back to JSON string
                string json = Encoding.UTF8.GetString(data);
                
                // Deserialize from JSON
                var packetData = JsonConvert.DeserializeObject<UnityPacket>(json);
                return packetData;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to deserialize packet: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get the size of the packet in bytes
        /// </summary>
        public int GetSizeBytes()
        {
            return ToBytes().Length;
        }
        
        /// <summary>
        /// Convert data to specific type
        /// </summary>
        public T GetData<T>()
        {
            if (Data == null)
                return default(T);
                
            try
            {
                if (Data is T directCast)
                    return directCast;
                    
                // Try JSON conversion for complex types
                string json = JsonConvert.SerializeObject(Data);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to convert packet data to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// Check if packet is valid
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(PacketType) && !string.IsNullOrEmpty(PlayerUID);
        }
        
        public override string ToString()
        {
            return $"UnityPacket[{PacketType}] from {PlayerUID}";
        }
    }
} 