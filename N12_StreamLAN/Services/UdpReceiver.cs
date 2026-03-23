using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server_StreamLAN.Services
{
    public class UdpReceiver : IDisposable
    {
        private readonly UdpClient _udp;
        private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(5);

        private readonly ConcurrentDictionary<string, ClientSession> _clients = new();

        public IEnumerable<ClientSession> Clients => _clients.Values;
        public int ClientCount => _clients.Count;

        public UdpReceiver(int port)
        {
            _udp = new UdpClient(port);
        }

 
        public async Task<(IPEndPoint Sender, byte[] JpegData, uint SeqNo, byte Flags)> ReceiveAsync()
        {
            var result = await _udp.ReceiveAsync();
            var ep     = result.RemoteEndPoint;

            byte[] jpeg;
            uint seqNo;
            byte flags;
            if (!PacketProtocol.Unpack(result.Buffer, out seqNo, out flags, out jpeg))
            {
                jpeg  = result.Buffer;
                seqNo = 0;
                flags = 0;
            }

            string key = ep.ToString();
            var session = _clients.GetOrAdd(key, _ => new ClientSession(ep));
            session.RecordFrame(seqNo);

            var stale = _clients
                .Where(kv => DateTime.UtcNow - kv.Value.LastSeen > ClientTimeout)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var k in stale) _clients.TryRemove(k, out _);

            return (ep, jpeg, seqNo, flags);
        }

        public void Dispose() => _udp?.Dispose();
    }
}
