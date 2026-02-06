using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace Client_StreamLAN.Services
{
    public static class NetworkInfo
    {
        public static string? GetLocalIPv4()
        {
            foreach(var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = ni.GetIPProperties();
                var addr = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (addr != null)
                    return addr.Address.ToString();
            }
            return null;
        }
    }
}
