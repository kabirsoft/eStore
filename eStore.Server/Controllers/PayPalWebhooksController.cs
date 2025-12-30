using eStore.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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

        public PayPalWebhooksController(
            AppDbContext db,
            IConfiguration config,
            ILogger<PayPalWebhooksController> logger,
            IHttpClientFactory httpFactory)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _http = httpFactory.CreateClient();
        }

        [HttpPost]
        public async Task<IActionResult> Handle()
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
            var eventType = doc.RootElement.GetProperty("event_type").GetString();

            if (eventType == "PAYMENT.CAPTURE.COMPLETED")
            {
                var resource = doc.RootElement.GetProperty("resource");

                // custom_id = orderId (we set this earlier)
                var orderIdStr = resource
                    .GetProperty("supplementary_data")
                    .GetProperty("related_ids")
                    .GetProperty("order_id")
                    .GetString();

                if (!Guid.TryParse(orderIdStr, out var orderId))
                {
                    _logger.LogWarning("PayPal webhook: invalid orderId");
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

        private async Task<bool> VerifyPayPalSignatureAsync(string body, string webhookId)
        {
            var token = Request.Headers["PAYPAL-TRANSMISSION-ID"].ToString();
            var time = Request.Headers["PAYPAL-TRANSMISSION-TIME"].ToString();
            var sig = Request.Headers["PAYPAL-TRANSMISSION-SIG"].ToString();
            var certUrl = Request.Headers["PAYPAL-CERT-URL"].ToString();
            var algo = Request.Headers["PAYPAL-AUTH-ALGO"].ToString();

            var accessToken = await GetPayPalAccessTokenAsync();

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
                "https://api-m.sandbox.paypal.com/v1/notifications/verify-webhook-signature");

            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            req.Content = JsonContent.Create(payload);

            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("verification_status").GetString() == "SUCCESS";
        }

        private async Task<string> GetPayPalAccessTokenAsync()
        {
            // reuse your existing PayPalAccessTokenService here
            throw new NotImplementedException();
        }
    }
}
