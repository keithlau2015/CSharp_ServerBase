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
            if (netClient == null || packet == null)
                return;

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
