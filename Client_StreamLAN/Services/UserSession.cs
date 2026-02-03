using System;
using System.Collections.Generic;
using System.Text;
using Client_StreamLAN.Services;

namespace Client_StreamLAN.Services
{
    public class UserSession
    {
        public static string AccessToken { get; set; } = string.Empty;
        public static string UserEmail { get; set; } = string.Empty;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);
    }
}
