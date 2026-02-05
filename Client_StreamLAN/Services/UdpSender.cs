using System.Net;
using System.Net.Sockets;

namespace Client_StreamLAN.Services
{
    public class UdpSender
    {
        private readonly UdpClient _udp;
        private readonly IPEndPoint _endPoint;

        public  UdpSender(string ip, int port)
        {
            _udp = new UdpClient();
            _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Send(byte[] data)
        {
            _udp.Send(data, data.Length, _endPoint);
        }
    }
}
