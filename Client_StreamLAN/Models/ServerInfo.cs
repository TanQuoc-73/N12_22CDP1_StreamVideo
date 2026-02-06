namespace Client_StreamLAN.Models
{
    public class ServerInfo
    {
        public string Name { get; set; } = "Server";
        public string Ip { get; set; } = "";
        public int Port { get; set; } = 9000;

        public override string ToString() => $"{Name} - {Ip}:{Port}";
    }
}
