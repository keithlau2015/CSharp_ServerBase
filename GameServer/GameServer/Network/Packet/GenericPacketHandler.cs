using System;
using System.Threading.Tasks;

namespace Network
{
    public class GenericPacketHandler<T> : PacketHandler
    {
        public GenericPacketHandler():base()
        {
            
        }

        public override async Task ReadPacket(NetClient netClient, object obj)
        {
            if (typeof(T) != obj.GetType())
            {
                Debug.DebugUtility.ErrorLog(this, $"Invalid Type[T => {typeof(T)}, obj => {obj.GetType()}]");
                return;
            }

            await base.ReadPacket(netClient, obj);
        }
    }
}
