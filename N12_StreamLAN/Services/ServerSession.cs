using System;

namespace Server_StreamLAN.Services
{
    public static class ServerSession
    {
        public static string? Username { get; set; }
        public static string? AccessToken { get; set; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);
    }
}
