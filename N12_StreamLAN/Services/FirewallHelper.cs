using System.Diagnostics;
using System.IO;

namespace Server_StreamLAN.Services
{

    public static class FirewallHelper
    {
        private const int VideoPort = 9000;
        private const int DiscoveryPort = 9001;
        private const int AudioPort = 9002;
        private const string RuleNameVideo = "StreamLAN-Video";
        private const string RuleNameDiscovery = "StreamLAN-Discovery";
        private const string RuleNameAudio = "StreamLAN-Audio";

        private static string FlagPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StreamLAN",
                "firewall_rules_added.txt");

        public static void EnsureFirewallRules()
        {
            if (HasRuleFlag())
                return;

            try
            {
                AddFirewallRulesElevated();
            }
            catch (Exception)
            {
             
            }
        }

        private static bool HasRuleFlag()
        {
            try
            {
                return File.Exists(FlagPath);
            }
            catch
            {
                return false;
            }
        }

        private static void SetRuleFlag()
        {
            try
            {
                var dir = Path.GetDirectoryName(FlagPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(FlagPath, DateTime.UtcNow.ToString("O"));
            }
            catch { }
        }

        private static void AddFirewallRulesElevated()
        {
            var cmd = $@"/c netsh advfirewall firewall add rule name=""{RuleNameVideo}"" dir=in action=allow protocol=UDP localport={VideoPort} && netsh advfirewall firewall add rule name=""{RuleNameDiscovery}"" dir=in action=allow protocol=UDP localport={DiscoveryPort} && netsh advfirewall firewall add rule name=""{RuleNameAudio}"" dir=in action=allow protocol=UDP localport={AudioPort}";
            var start = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                UseShellExecute = true,
                Verb = "runas", 
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var p = Process.Start(start);
            if (p == null)
                return; 

            p.WaitForExit(TimeSpan.FromSeconds(15));
            if (p.ExitCode == 0)
                SetRuleFlag();
        }
    }
}
