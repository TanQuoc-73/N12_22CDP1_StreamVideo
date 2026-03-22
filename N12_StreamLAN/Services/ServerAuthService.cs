using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Server_StreamLAN.Services
{
    /// <summary>
    /// Simple server-side authentication service.
    /// Currently uses an in-memory admin account read from environment variables
    /// `SERVER_ADMIN_USER` and `SERVER_ADMIN_PWD` with defaults `admin` / `admin123`.
    /// Replace or extend to use a proper user store (database, LDAP, etc.).
    /// </summary>
    public class ServerAuthService
    {
        private readonly Dictionary<string, string> _users = new();

        public ServerAuthService()
        {
            string adminUser = Environment.GetEnvironmentVariable("SERVER_ADMIN_USER") ?? "admin";
            string adminPwd  = Environment.GetEnvironmentVariable("SERVER_ADMIN_PWD")  ?? "admin123";
            _users[adminUser] = adminPwd;
        }

        public bool Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            return _users.TryGetValue(username, out var pwd) && pwd == password;
        }

        // Authenticate via Supabase password grant (email + password).
        // Returns (ok, email) on success.
        public async Task<(bool ok, string? email)> AuthenticateWithSupabaseAsync(string email, string password)
        {
            string supabaseUrl = Environment.GetEnvironmentVariable("SERVER_SUPABASE_URL") ?? "https://spuuejvuiuubrddlzqxa.supabase.co";

            // Prefer anon key for password grant; fallback to service role if provided.
            string apiKey = Environment.GetEnvironmentVariable("SERVER_SUPABASE_ANON_KEY") ?? Environment.GetEnvironmentVariable("SERVER_SUPABASE_SERVICE_ROLE") ?? string.Empty;
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Missing Supabase API key. Set SERVER_SUPABASE_ANON_KEY or SERVER_SUPABASE_SERVICE_ROLE.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("apikey", apiKey);

            var body = new Dictionary<string, string>()
            {
                { "email", email },
                { "password", password }
            };

            HttpResponseMessage resp;
            try
            {
                resp = await http.PostAsync($"{supabaseUrl}/auth/v1/token?grant_type=password", new FormUrlEncodedContent(body));
            }
            catch (Exception)
            {
                return (false, null);
            }

            if (!resp.IsSuccessStatusCode) return (false, null);

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string? accessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            string? userEmail = null;
            if (doc.RootElement.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("email", out var emailEl))
                userEmail = emailEl.GetString();

            if (!string.IsNullOrEmpty(accessToken))
            {
                return (true, userEmail);
            }

            return (false, null);
        }
    }
}
