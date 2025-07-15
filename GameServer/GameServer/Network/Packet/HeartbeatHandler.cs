using System.Threading.Tasks;

namespace Network
{
    public class HeartbeatHandler : PacketHandlerBase
    {
        public override async Task ReadPacket(NetClient netClient, Packet packet)
        {
            if (netClient == null || packet == null)
            {
                Debug.DebugUtility.ErrorLog($"Params Null[NetClient => {netClient == null}, packet => {packet == null}]");
                return;
            }
            if (!netClient.isAlive)
            {
                Debug.DebugUtility.ErrorLog($"NetClient not alive");
                return;
            }

            await Task.Run(() => {
                using (Packet response = new Packet("ResponseHeartbeat"))
                {
                    //Current Server Time
                    netClient.Send(response);
                }
            });
        }
    }
}
