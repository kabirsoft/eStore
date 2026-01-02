using eStore.Server.Data;
using eStore.Server.Payments.PayPal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalWebhooksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<PayPalWebhooksController> _logger;
        private readonly HttpClient _http;
        private readonly PayPalAccessTokenService _tokens;
        private readonly PayPalOptions _opt;

        public PayPalWebhooksController(
            AppDbContext db,
            IConfiguration config,
            ILogger<PayPalWebhooksController> logger,
            IHttpClientFactory httpFactory,
            PayPalAccessTokenService tokens,
            PayPalOptions opt)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _http = httpFactory.CreateClient();
            _tokens = tokens;
            _opt = opt;
        }

        [HttpPost]
        public async Task<IActionResult> Handle()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var webhookId = _config["PayPal:WebhookId"];
                if (string.IsNullOrWhiteSpace(webhookId))
                    return StatusCode(500, "Missing PayPal:WebhookId");

                if (!await VerifyPayPalSignatureAsync(body, webhookId))
                {
                    _logger.LogWarning("PayPal webhook signature verification failed");
                    return BadRequest();
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("event_type", out var et))
                    return Ok();

                var eventType = et.GetString();

                if (eventType == "PAYMENT.CAPTURE.COMPLETED")
                {
                    var resource = doc.RootElement.GetProperty("resource");

                    string? orderIdStr = null;

                    // Preferred: resource.custom_id
                    if (resource.TryGetProperty("custom_id", out var customIdEl))
                        orderIdStr = customIdEl.GetString();

                    // Fallback: sometimes custom_id might live elsewhere depending on event structure
                    if (string.IsNullOrWhiteSpace(orderIdStr) &&
                         resource.TryGetProperty("purchase_units", out var pus) &&
                         pus.ValueKind == JsonValueKind.Array &&
                         pus.GetArrayLength() > 0 &&
                         pus[0].TryGetProperty("custom_id", out var puCustom))
                    {
                        orderIdStr = puCustom.GetString();
                    }

                    _logger.LogInformation("PayPal webhook received. event_type={EventType}, custom_id={CustomId}", eventType, orderIdStr);

                    if (!Guid.TryParse(orderIdStr, out var orderId))
                    {
                        _logger.LogWarning("PayPal webhook: missing/invalid custom_id orderId. custom_id={CustomId}", orderIdStr);
                        return Ok();
                    }

                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order == null || order.PaymentStatus == "Paid")
                        return Ok();

                    order.PaymentProvider = "PayPal";
                    order.PaymentStatus = "Paid";
                    order.PaidUtc = DateTime.UtcNow;
                    order.Status = "Completed";

                    var tx = await _db.PaymentTransactions
                        .FirstOrDefaultAsync(t => t.OrderId == order.Id && t.Provider == "PayPal");

                    if (tx != null)
                    {
                        tx.Status = "Succeeded";
                        tx.UpdatedUtc = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in PayPal webhook");
                return StatusCode(500);
            }
        }

        private async Task<bool> VerifyPayPalSignatureAsync(string body, string webhookId)
        {
            var token = Request.Headers["PAYPAL-TRANSMISSION-ID"].ToString();
            var time = Request.Headers["PAYPAL-TRANSMISSION-TIME"].ToString();
            var sig = Request.Headers["PAYPAL-TRANSMISSION-SIG"].ToString();
            var certUrl = Request.Headers["PAYPAL-CERT-URL"].ToString();
            var algo = Request.Headers["PAYPAL-AUTH-ALGO"].ToString();

            var accessTokenValue = await _tokens.GetAccessTokenAsync();

            var payload = new
            {
                auth_algo = algo,
                cert_url = certUrl,
                transmission_id = token,
                transmission_sig = sig,
                transmission_time = time,
                webhook_id = webhookId,
                webhook_event = JsonSerializer.Deserialize<object>(body)
            };

            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_opt.BaseUrl.TrimEnd('/')}/v1/notifications/verify-webhook-signature");

            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessTokenValue);

            req.Content = JsonContent.Create(payload);

            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            _logger.LogInformation("PayPal verify response. Status={Status} Body={Body}", (int)resp.StatusCode, json);

            // If PayPal returns 400/401/etc, signature is NOT verified
            if (!resp.IsSuccessStatusCode)
                return false;

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("verification_status", out var vs))
                return false;

            return string.Equals(vs.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase);

        }
    }
}
