using System.Net;
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

        /// <summary>
        /// Receive a datagram and return both the payload and the remote endpoint (client IP:port).
        /// </summary>
        public async Task<(byte[] Buffer, IPEndPoint RemoteEndPoint)> ReceiveAsync()
        {
            var result = await _udp.ReceiveAsync();
            return (result.Buffer, result.RemoteEndPoint);
        }
    }
}
