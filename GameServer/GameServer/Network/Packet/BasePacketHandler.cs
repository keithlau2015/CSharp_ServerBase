using System.Threading.Tasks;

namespace Network
{
    public abstract class PacketHandlerBase
    {
        public abstract Task ReadPacket(NetClient netClient, Packet packet);
    }
}
