using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SamplePlugin
{
    public static class AIIntegrationManager
    {
        private static readonly HttpClient _httpClient = new();
        private static TimeSpan _elapsed = TimeSpan.Zero;
        private static readonly TimeSpan _sendInterval = TimeSpan.FromSeconds(10);

        public static void Update(TimeSpan delta)
        {
            _elapsed += delta;
            if (_elapsed >= _sendInterval)
            {
                _elapsed = TimeSpan.Zero;
                _ = PollForCommandsAsync();
            }
        }

        public static async Task SendEventAsync(string eventName, object payload)
        {
            var json = JsonSerializer.Serialize(new
            {
                Event = eventName,
                data = payload
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://127.0.0.1:60303/", content);
                if (!response.IsSuccessStatusCode)
                {
                    Plugin.Log.Error($"Failed to send event to AI: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error sending data to AI console: " + ex.Message);
            }
        }

        public static async Task PollForCommandsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://127.0.0.1:60303/plugin-command");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        Plugin.Log.Information("AI says: " + content);
                        ChatBubbleManager.Bubble(content);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Error polling AI console: " + ex.Message);
            }
        }
    }
}
