using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client_StreamLAN.Services
{
    public class UdpSender : IDisposable
    {
        private UdpClient _udp;
        private readonly IPEndPoint _endPoint;

        public string ServerIp   { get; }
        public int    ServerPort { get; }

        public UdpSender(string ip, int port)
        {
            ServerIp   = ip;
            ServerPort = port;
            _udp       = new UdpClient();
            _endPoint  = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Send(byte[] data)
            => _udp.Send(data, data.Length, _endPoint);

        public async Task SendAsync(byte[] data)
            => await _udp.SendAsync(data, data.Length, _endPoint);

        /// <summary>Re-creates the internal socket (call after a network error).</summary>
        public void Reconnect()
        {
            try { _udp.Close(); _udp.Dispose(); } catch { }
            _udp = new UdpClient();
        }

        public void Dispose()
        {
            try { _udp.Close(); _udp.Dispose(); } catch { }
        }
    }
}
