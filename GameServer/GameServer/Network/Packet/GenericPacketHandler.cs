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

        public override Task ReadPacket(NetClient netClient, Packet packet)
        {
            if(netClient == null || packet == null)
            {
                Debug.DebugUtility.ErrorLog(this, $"Params Null[NetClient => {netClient == null}, packet => {packet == null}]");
                return;
            }

            if(packet.UnreadLength() == 0)
            {
                Debug.DebugUtility.ErrorLog(this, $"Packet unreadLength is 0");
                return;
            }
            
            T obj = packet.ReadObject<T>();
            if(obj == null)
            {
                Debug.DebugUtility.ErrorLog(this, $"obj is null");
                return;
            }
            this.cb?.Invoke(netClient, obj);
        }
    }
}
