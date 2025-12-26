using eStore.Server.Data;
using eStore.Shared.Dtos;
using eStore.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public PaymentsController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("stripe/create-checkout-session")]
        public async Task<ActionResult<StripeCheckoutSessionDto>> CreateStripeCheckoutSession(StartStripeCheckoutDto dto)
        {
            var order = await _db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == dto.OrderId);
            if (order is null) return NotFound("Order not found.");

            if (order.PaymentStatus == "Paid")
                return BadRequest("Order already paid.");

            var secretKey = _config["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                return StatusCode(500, "Missing Stripe:SecretKey config.");

            StripeConfiguration.ApiKey = secretKey;

            // Create a payment transaction row (your DB)
            var tx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = "Stripe",
                Status = "Created",
                AmountOre = order.TotalOre,
                Currency = order.Currency,
                CreatedUtc = DateTime.UtcNow
            };
            _db.PaymentTransactions.Add(tx);

            // Update order payment state to pending
            order.PaymentProvider = "Stripe";
            order.PaymentStatus = "Pending";
            order.PaymentReference = tx.Id.ToString();
            await _db.SaveChangesAsync();

            // Stripe expects amount in the smallest unit (øre)
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{_config["ClientBaseUrl"]}/payment/success?orderId={order.Id}",
                CancelUrl = $"{_config["ClientBaseUrl"]}/payment/cancel?orderId={order.Id}",
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Quantity = order.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = order.Currency.ToLowerInvariant(),
                        UnitAmount = order.UnitPriceOre,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = order.Product?.Name ?? "Product"
                        }
                    }
                }
            },
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["transactionId"] = tx.Id.ToString()
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Store Stripe session id as provider reference
            tx.ProviderReference = session.Id;
            tx.Status = "Pending";
            tx.UpdatedUtc = DateTime.UtcNow;

            // (optional) store session id on order too
            order.PaymentReference = session.Id;

            await _db.SaveChangesAsync();

            return Ok(new StripeCheckoutSessionDto { Url = session.Url });
        }
    }
}
