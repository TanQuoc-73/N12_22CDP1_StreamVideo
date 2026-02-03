using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Client_StreamLAN.Services;


namespace Client_StreamLAN.Services
{
    public class SupabaseAuthService
    {
        private const string SUPABASE_URL = "https://spuuejvuiuubrddlzqxa.supabase.co";
        private const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNwdXVlanZ1aXV1YnJkZGx6cXhhIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzAwODc5MjQsImV4cCI6MjA4NTY2MzkyNH0.CcsxCZjD8sQRPZor8wyYJ7enlhYg01fByWGjk_Dvb3U";

        private readonly HttpClient _http;

        public SupabaseAuthService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var url = $"{SUPABASE_URL}/auth/v1/token?grant_type=password";

            var body = new
            {
                email = email,
                password = password
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            string result = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(result);

            UserSession.AccessToken = doc.RootElement.GetProperty("access_token").GetString();
            UserSession.UserEmail = email;

            return true;
        }
    }
}
