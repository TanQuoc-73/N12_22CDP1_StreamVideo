using Client_StreamLAN.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Linq;


namespace Client_StreamLAN.Services
{
    public class ServerDiscovery
    {
        public async Task<List<ServerInfo>> DiscoverAsync(int timeoutMs = 1500)
        {
            var raw = new List<ServerInfo>();

            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            byte[] msg = Encoding.UTF8.GetBytes("DISCOVER_SERVER");

            try { await udp.SendAsync(msg, msg.Length, new IPEndPoint(IPAddress.Loopback, 9001)); } catch { }

            foreach (var addr in GetSubnetBroadcastAddresses())
            {
                try { await udp.SendAsync(msg, msg.Length, new IPEndPoint(addr, 9001)); } catch { }
            }

            try { await udp.SendAsync(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, 9001)); } catch { }

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    if (udp.Available > 0)
                    {
                        var result = await udp.ReceiveAsync();
                        string text = Encoding.UTF8.GetString(result.Buffer);

                        if (text.StartsWith("SERVER_HERE"))
                        {
                            var parts = text.Split('|');
                            string name = parts.Length > 1 ? parts[1] : "Server";
                            int port = (parts.Length > 2 && int.TryParse(parts[2], out int p)) ? p : 9000;

                            raw.Add(new ServerInfo
                            {
                                Name = name,
                                Ip = result.RemoteEndPoint.Address.ToString(),
                                Port = port
                            });
                        }
                    }
                }
                catch { }

                await Task.Delay(50);
            }

            return DeduplicateAndRank(raw);
        }

        private static List<ServerInfo> DeduplicateAndRank(List<ServerInfo> raw)
        {
            var localPrivate = GetLocalPrivateAddresses().ToList();

            bool IsPrivate(string ip) =>
                IPAddress.TryParse(ip, out var addr) &&
                (ip.StartsWith("10.") ||
                 ip.StartsWith("192.168.") ||
                 (ip.StartsWith("172.") && int.TryParse(ip.Split('.')[1], out int b) && b >= 16 && b <= 31) ||
                 ip == "127.0.0.1");

            int Score(ServerInfo s)
            {
                if (s.Ip == "127.0.0.1") return 3; 
                foreach (var (localIp, mask) in localPrivate)
                {
                    if (SameSubnet(s.Ip, localIp, mask)) return 0;
                }
                if (IsPrivate(s.Ip)) return 1;
                return 2; // public IP
            }

            return raw
                .Where(s => IsPrivate(s.Ip))         
                .OrderBy(s => Score(s))
                .GroupBy(s => s.Name)                 
                .Select(g => g.First())                
                .ToList();
        }

        private static bool SameSubnet(string ip1, string ip2, string mask)
        {
            if (!IPAddress.TryParse(ip1, out var a) ||
                !IPAddress.TryParse(ip2, out var b) ||
                !IPAddress.TryParse(mask, out var m)) return false;
            var ab = a.GetAddressBytes();
            var bb = b.GetAddressBytes();
            var mb = m.GetAddressBytes();
            for (int i = 0; i < 4; i++)
                if ((ab[i] & mb[i]) != (bb[i] & mb[i])) return false;
            return true;
        }

        private static IEnumerable<(string ip, string mask)> GetLocalPrivateAddresses()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    yield return (ua.Address.ToString(), ua.IPv4Mask.ToString());
                }
            }
        }

        private static IEnumerable<IPAddress> GetSubnetBroadcastAddresses()
        {
            foreach (var (ip, mask) in GetLocalPrivateAddresses())
            {
                if (!IPAddress.TryParse(ip, out var addr) ||
                    !IPAddress.TryParse(mask, out var maskAddr)) continue;
                var ipB = addr.GetAddressBytes();
                var mB = maskAddr.GetAddressBytes();
                var bc = new byte[4];
                for (int i = 0; i < 4; i++) bc[i] = (byte)(ipB[i] | ~mB[i]);
                yield return new IPAddress(bc);
            }
        }
    }
}
