using eStore.Server.Data;
using eStore.Server.Payments.Vipps;
using eStore.Server.Services.PaymentOrchestrator;
using eStore.Shared.Dtos;
using eStore.Shared.Enums;
using eStore.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IPaymentOrchestrator _payments;

        public PaymentsController(AppDbContext db, IConfiguration config, IPaymentOrchestrator payments)
        {
            _db = db;
            _config = config;
            _payments = payments;
        }

        [HttpPost("create")]
        public async Task<ActionResult<CreatePaymentResponse>> Create(CreatePaymentRequest req)
        {
            var url = await _payments.CreateCheckoutAsync(req.OrderId, req.Provider);
            return Ok(new CreatePaymentResponse(url));
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

        [HttpGet("vipps/poll")]
        public async Task<ActionResult<object>> PollVipps([FromQuery] Guid orderId, [FromServices] VippsEpaymentClient vipps)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return NotFound("Order not found.");

            if (order.PaymentProvider != "Vipps" || string.IsNullOrWhiteSpace(order.PaymentReference))
                return BadRequest("Order is not a Vipps payment.");

            var reference = order.PaymentReference;

            using var doc = await vipps.GetPaymentAsync(reference);

            // Status field name depends on Vipps response; we keep raw json + you can map later
            // For UI you can just display the json or pick doc.RootElement.GetProperty("state") etc.
            var raw = doc.RootElement.GetRawText();

            return Ok(new { reference, raw });
        }
    }
}
