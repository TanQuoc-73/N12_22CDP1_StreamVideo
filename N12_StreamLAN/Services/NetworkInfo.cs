using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Server_StreamLAN.Services
{
    public static class NetworkInfo
    {
        public static string? GetLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                string desc = ni.Description.ToLower();
                if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("tunnel") || 
                    desc.Contains("vmware") || desc.Contains("virtualbox") || desc.Contains("docker"))
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
