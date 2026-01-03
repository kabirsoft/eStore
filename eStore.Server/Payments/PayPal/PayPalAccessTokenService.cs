using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;

namespace eStore.Server.Payments.PayPal
{
    public class PayPalAccessTokenService
    {
        private const string CacheKey = "paypal_access_token";
        private readonly HttpClient _http;
        private readonly PayPalOptions _opt;
        private readonly IMemoryCache _cache;

        public PayPalAccessTokenService(HttpClient http, PayPalOptions opt, IMemoryCache cache)
        {
            _http = http;
            _opt = opt;
            _cache = cache;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_cache.TryGetValue(CacheKey, out string token) && !string.IsNullOrWhiteSpace(token))
                return token;

            if (!_opt.IsConfigured)
                throw new InvalidOperationException("PayPal is not configured (missing ClientId/ClientSecret/BaseUrl).");

            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_opt.ClientId}:{_opt.ClientSecret}"));
            using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

            using var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"PayPal token error: {(int)resp.StatusCode} {json}");

            // tiny parsing without creating DTOs
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("PayPal access_token missing.");

            // Cache slightly less than expiry
            _cache.Set(CacheKey, accessToken, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60)));

            return accessToken;
        }
    }
}
