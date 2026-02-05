using System.Net.Sockets;

namespace Server_StreamLAN.Services
{
    public class UdpReceiver
    {
        private readonly UdpClient _udp;

        public UdpReceiver(int port)
        {
            _udp = new UdpClient(port);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            var result = await _udp.ReceiveAsync();
            return result.Buffer;
        }
    }
}
