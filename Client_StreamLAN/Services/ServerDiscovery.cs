using Client_StreamLAN.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;


namespace Client_StreamLAN.Services
{
    public class ServerDiscovery
    {
        public async Task<List<ServerInfo>> DiscoverAsync(int timeoutMs = 1200)
        {
            List<ServerInfo> servers = new();

            using var udp = new UdpClient();
            udp.EnableBroadcast = true;

            byte[] msg = Encoding.UTF8.GetBytes("DISCOVER_SERVER");

            await udp.SendAsync(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, 9001));

            await udp.SendAsync(msg, msg.Length, new IPEndPoint(IPAddress.Loopback, 9001));

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
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

                        var info = new ServerInfo
                        {
                            Name = name,
                            Ip = result.RemoteEndPoint.Address.ToString(),
                            Port = port
                        };

                        if (!servers.Any(s => s.Ip == info.Ip && s.Port == info.Port))
                            servers.Add(info);
                    }
                }

                await Task.Delay(50);
            }

            return servers;
        }
    }
}
