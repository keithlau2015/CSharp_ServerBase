using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Network
{
    public class PacketHandlers : PacketHandlerBase
    {
        protected List<PacketHandlerBase> handlers = new List<PacketHandlerBase>();
        public PacketHandlers(params PacketHandlerBase[] para):base()
        {
            handlers.AddRange(handlers);
        }

        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            if(netClient == null || packet == null)
            {
                Debug.DebugUtility.ErrorLog(this, $"Params Null[NetClient => {netClient == null}, packet => {packet == null}]");
                return;
            }
            if(!netClient.IsAlive)
            {
                Debug.DebugUtility.ErrorLog(this, $"NetClient not alive");
                return;
            }
            if(packet.UnreadLength() == 0)
            {
                Debug.DebugUtility.ErrorLog(this, $"Packet unreadLength is 0");
                return;
            }
            using (packet)
            {
                foreach (PacketHandlerBase handler in handlers)
                {
                    await handler.ReadPacket(netClient, packet);
                }
            }
        }
    }
}
