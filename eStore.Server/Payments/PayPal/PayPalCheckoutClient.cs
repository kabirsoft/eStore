using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace eStore.Server.Payments.PayPal
{
    public class PayPalCheckoutClient
    {
        private readonly HttpClient _http;
        private readonly PayPalAccessTokenService _tokens;

        public PayPalCheckoutClient(HttpClient http, PayPalAccessTokenService tokens)
        {
            _http = http;
            _tokens = tokens;
        }

        public async Task<string> CreateOrderAndGetApprovalUrlAsync(
            string currency,
            decimal amount, // major units (e.g., 199.00 NOK)
            string returnUrl,
            string cancelUrl,
            string brandName,
            Dictionary<string, string>? metadata = null)
        {
            var token = await _tokens.GetAccessTokenAsync();

            using var req = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // PayPal wants amount in major units as a string with 2 decimals typically
            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency.ToUpperInvariant(),
                            value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        },
                        custom_id = metadata != null && metadata.TryGetValue("orderId", out var oid) ? oid : null
                    }
                },
                application_context = new
                {
                    brand_name = brandName,
                    user_action = "PAY_NOW",
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            var respJson = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"PayPal create order error: {(int)resp.StatusCode} {respJson}");

            using var doc = JsonDocument.Parse(respJson);

            // Find "approve" link
            var links = doc.RootElement.GetProperty("links").EnumerateArray();
            foreach (var l in links)
            {
                if (l.TryGetProperty("rel", out var rel) && rel.GetString() == "approve")
                    return l.GetProperty("href").GetString() ?? throw new Exception("PayPal approve href missing.");
            }

            throw new Exception("PayPal approval link not found in response.");
        }
    }
}
