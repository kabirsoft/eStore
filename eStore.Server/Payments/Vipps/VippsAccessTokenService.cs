using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace eStore.Server.Payments.Vipps
{
    public sealed class VippsAccessTokenService
    {
        private const string CacheKey = "VippsAccessToken";
        private readonly HttpClient _http;
        private readonly VippsOptions _opt;
        private readonly ILogger<VippsAccessTokenService> _logger;
        private readonly IMemoryCache _cache;

        private string? _token;
        private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

        public VippsAccessTokenService(HttpClient http, VippsOptions opt, ILogger<VippsAccessTokenService> logger, IMemoryCache cache)
        {
            _http = http;
            _opt = opt;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> GetTokenAsync(CancellationToken ct = default)
        {            
            //if (!string.IsNullOrWhiteSpace(_token) && DateTimeOffset.UtcNow < _expiresAtUtc.AddMinutes(-2))
            //    return _token!;

            if (_cache.TryGetValue<string>(CacheKey, out var cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
                return cachedToken;

            if (!_opt.IsConfigured)
                throw new InvalidOperationException("Vipps is not configured (missing keys).");

            using var req = new HttpRequestMessage(HttpMethod.Post, "/accesstoken/get");
            req.Headers.TryAddWithoutValidation("client_id", _opt.ClientId);
            req.Headers.TryAddWithoutValidation("client_secret", _opt.ClientSecret);
            req.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _opt.SubscriptionKey);
            req.Headers.TryAddWithoutValidation("Merchant-Serial-Number", _opt.MerchantSerialNumber);

            req.Headers.TryAddWithoutValidation("Vipps-System-Name", _opt.SystemName);
            req.Headers.TryAddWithoutValidation("Vipps-System-Version", _opt.SystemVersion);
            req.Headers.TryAddWithoutValidation("Vipps-System-Plugin-Name", _opt.PluginName);
            req.Headers.TryAddWithoutValidation("Vipps-System-Plugin-Version", _opt.PluginVersion);

            req.Content = new StringContent(""); // Vipps expects empty body

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Vipps accesstoken/get failed. Status={Status}. Body={Body}", (int)res.StatusCode, json);
                throw new InvalidOperationException($"Vipps token request failed: {(int)res.StatusCode}");
            }

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresInStr = doc.RootElement.GetProperty("expires_in").GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Vipps token response missing access_token.");

            // expires_in is returned as string in docs/examples
            _ = int.TryParse(expiresInStr, out var expiresInSec);
            if (expiresInSec <= 0) expiresInSec = 3600;

            var cacheTtl = TimeSpan.FromSeconds(Math.Max(60, expiresInSec - 120));
            _cache.Set(CacheKey, accessToken, cacheTtl);

            return accessToken!;
        }
    }
}
