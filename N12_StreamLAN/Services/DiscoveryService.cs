using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server_StreamLAN.Services
{
    public class DiscoveryService
    {
        private readonly UdpClient _udp;
        private const int DISCOVERY_PORT = 9001;

        public DiscoveryService()
        {
            _udp = new UdpClient(DISCOVERY_PORT);
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _udp.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(result.Buffer);

                    if (msg == "DISCOVER_SERVER")
                    {
                        string reply = "SERVER_HERE|StreamServer|9000";
                        byte[] data = Encoding.UTF8.GetBytes(reply);

                        await _udp.SendAsync(data, data.Length, result.RemoteEndPoint);
                    }   
                }
            });
        }
    }
}
