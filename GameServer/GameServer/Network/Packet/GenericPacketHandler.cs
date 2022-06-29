using System;
using System.Threading.Tasks;

namespace Network
{
    public class GenericPacketHandler<T> : PacketHandlerBase
    {
        private Action<NetClient, T> cb;
        public GenericPacketHandler(Action<NetClient, T> cb)
        {
            this.cb = cb;
        }

        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            if(netClient == null || packet == null)
            {
                Debug.DebugUtility.ErrorLog($"Params Null[NetClient => {netClient == null}, packet => {packet == null}]");
                return;
            }
            if(!netClient.IsAlive)
            {
                Debug.DebugUtility.ErrorLog($"NetClient not alive");
                return;
            }
            if(packet.UnreadLength() == 0)
            {
                Debug.DebugUtility.ErrorLog($"Packet unreadLength is 0");
                return;
            }
            using(packet)
            {
                T obj = (T)packet.ReadObject<T>();
                if(obj == null)
                {
                    Debug.DebugUtility.ErrorLog($"obj is null");
                    return;
                }
                await Task.Run(() => { this.cb?.Invoke(netClient, obj); });
            }
        }
    }
}
