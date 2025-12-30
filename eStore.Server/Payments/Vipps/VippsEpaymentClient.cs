using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace eStore.Server.Payments.Vipps
{
    public sealed class VippsEpaymentClient
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly VippsOptions _opt;
        private readonly VippsAccessTokenService _token;
        private readonly ILogger<VippsEpaymentClient> _logger;

        public VippsEpaymentClient(HttpClient http, VippsOptions opt, VippsAccessTokenService token, ILogger<VippsEpaymentClient> logger)
        {
            _http = http;
            _opt = opt;
            _token = token;
            _logger = logger;
        }

        public async Task<(string RedirectUrl, string Reference)> CreatePaymentAsync(
            string reference,
            long amountOre,
            string currency,
            string returnUrl,
            string? phoneNumberE164DigitsOnly,
            string? description,
            string idempotencyKey,
            CancellationToken ct = default)
        {
            var token = await _token.GetTokenAsync(ct);

            using var req = new HttpRequestMessage(HttpMethod.Post, "/epayment/v1/payments");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            req.Headers.Add("Ocp-Apim-Subscription-Key", _opt.SubscriptionKey);
            req.Headers.Add("Merchant-Serial-Number", _opt.MerchantSerialNumber);
            req.Headers.Add("Idempotency-Key", idempotencyKey);

            req.Headers.Add("Vipps-System-Name", _opt.SystemName);
            req.Headers.Add("Vipps-System-Version", _opt.SystemVersion);
            req.Headers.Add("Vipps-System-Plugin-Name", _opt.PluginName);
            req.Headers.Add("Vipps-System-Plugin-Version", _opt.PluginVersion);

            var body = new
            {
                amount = new { value = amountOre, currency = currency },
                paymentMethod = new { type = "WALLET" },
                reference = reference,
                paymentDescription = description ?? "",
                returnUrl = returnUrl,
                userFlow = "WEB_REDIRECT",
                customer = string.IsNullOrWhiteSpace(phoneNumberE164DigitsOnly)
                    ? null
                    : new { phoneNumber = phoneNumberE164DigitsOnly }
            };

            var json = JsonSerializer.Serialize(body, JsonOpts);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req, ct);
            var resJson = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Vipps create payment failed. Status={Status}. Body={Body}", (int)res.StatusCode, resJson);
                throw new InvalidOperationException($"Vipps create payment failed: {(int)res.StatusCode}");
            }

            using var doc = JsonDocument.Parse(resJson);
            var redirectUrl = doc.RootElement.GetProperty("redirectUrl").GetString();

            if (string.IsNullOrWhiteSpace(redirectUrl))
                throw new InvalidOperationException("Vipps response missing redirectUrl.");

            return (redirectUrl!, reference);
        }

        public async Task<JsonDocument> GetPaymentAsync(string reference, CancellationToken ct = default)
        {
            var token = await _token.GetTokenAsync(ct);

            using var req = new HttpRequestMessage(HttpMethod.Get, $"/epayment/v1/payments/{Uri.EscapeDataString(reference)}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Add("Ocp-Apim-Subscription-Key", _opt.SubscriptionKey);
            req.Headers.Add("Merchant-Serial-Number", _opt.MerchantSerialNumber);

            req.Headers.Add("Vipps-System-Name", _opt.SystemName);
            req.Headers.Add("Vipps-System-Version", _opt.SystemVersion);
            req.Headers.Add("Vipps-System-Plugin-Name", _opt.PluginName);
            req.Headers.Add("Vipps-System-Plugin-Version", _opt.PluginVersion);

            var res = await _http.SendAsync(req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Vipps get payment failed. Status={Status}. Body={Body}", (int)res.StatusCode, json);
                throw new InvalidOperationException($"Vipps get payment failed: {(int)res.StatusCode}");
            }

            return JsonDocument.Parse(json);
        }
    }
}
