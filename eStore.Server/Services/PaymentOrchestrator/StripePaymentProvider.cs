using eStore.Server.Data;
using eStore.Shared.Enums;
using eStore.Shared.Models;
using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public class StripePaymentProvider : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.Stripe;

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public StripePaymentProvider(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<string> CreateCheckoutAsync(Guid orderId)
        {
            var order = await _db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) throw new ArgumentException("Order not found.");

            if (order.PaymentStatus == "Paid")
                throw new InvalidOperationException("Order already paid.");

            var secretKey = _config["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Missing Stripe:SecretKey config.");

            var clientBaseUrl = _config["ClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
                throw new InvalidOperationException("Missing config: ClientBaseUrl");

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

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{clientBaseUrl}/payment/success?orderId={order.Id}",
                CancelUrl = $"{clientBaseUrl}/payment/cancel?orderId={order.Id}",
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

            // Store session on order too (useful)
            order.PaymentReference = session.Id;

            await _db.SaveChangesAsync();

            return session.Url ?? throw new InvalidOperationException("Stripe session URL missing.");
        }
    }
}

