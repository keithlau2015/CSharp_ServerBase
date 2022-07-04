using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (!netClient.IsAlive)
            {
                Debug.DebugUtility.ErrorLog($"NetClient not alive");
                return;
            }
            if (packet.UnreadLength() == 0)
            {
                Debug.DebugUtility.ErrorLog($"Packet unreadLength is 0");
                return;
            }

            await Task.Run(() => {
                Packet response = new Packet("ResponseHeartbeat");
                //Current Server Time
                netClient.Send(response);
            });
        }
    }
}
