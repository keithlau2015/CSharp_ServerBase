namespace Network
{
    public class Server
    {
        private long ticks = 0;
        private TcpListener tcpListener = null;
        private UdpListener udpListener = null;

#region Port
        private Int32 tcpPort = 0;
        private Int32 udpPort = 0;
#endregion

        public Server(string ipAddress = "127.0.0.1", Int32 tcpPort = 439500, Int32 udpPort = 539500)
        {
            this.tcpPort = tcpPort;
            this.udpPort = udpPort;
            tcpListener = new TcpListener(ipAddress, this.tcpPort);
            udpListener = new UdpListener(this.udpPort);
        }

        public void ExecuteStartUpProc()
        {
            if(this.tcpPort == 0 || this.udpPort == 0)
                return;

            //TCP
            tcpListener.Start();

            //UDP
            
        }

        public void Terminate()
        {
            tcpListener.Stop();
        }
    }
}