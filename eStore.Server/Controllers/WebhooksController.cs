using Microsoft.AspNetCore.Http;
using eStore.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(AppDbContext db, IConfiguration config, ILogger<WebhooksController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> Stripe()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
                return StatusCode(500, "Missing Stripe:WebhookSecret config.");

            try
            {
                var stripeSignature = Request.Headers["Stripe-Signature"];
                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

                // ✅ Payment completed
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session is null) return Ok();

                    var orderIdStr = session.Metadata?["orderId"];
                    if (!Guid.TryParse(orderIdStr, out var orderId))
                    {
                        _logger.LogWarning("Stripe webhook: orderId missing/invalid in metadata. SessionId={SessionId}", session.Id);
                        return Ok();
                    }

                    var txIdStr = session.Metadata?["transactionId"];
                    Guid.TryParse(txIdStr, out var txId);

                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order is null)
                    {
                        _logger.LogWarning("Stripe webhook: Order not found. OrderId={OrderId}", orderId);
                        return Ok();
                    }

                    if (order.PaymentStatus == "Paid")
                        return Ok();

                    // update order payment state
                    order.PaymentProvider = "Stripe";
                    order.PaymentStatus = "Paid";
                    order.PaidUtc = DateTime.UtcNow;
                    order.PaymentReference = session.Id;
                    order.Status = "Completed";

                    // update transaction if we have it
                    var tx = txId != Guid.Empty
                        ? await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == txId)
                        : await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderReference == session.Id);

                    if (tx != null)
                    {
                        tx.Status = "Succeeded";
                        tx.ProviderReference = session.Id;
                        tx.UpdatedUtc = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync();
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook processing failed.");
                return StatusCode(500);
            }
        }
    }
}
