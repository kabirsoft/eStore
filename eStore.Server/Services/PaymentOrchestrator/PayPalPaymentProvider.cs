using eStore.Server.Data;
using eStore.Server.Payments.PayPal;
using eStore.Shared.Enums;
using eStore.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public class PayPalPaymentProvider : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.PayPal;

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly PayPalOptions _opt;
        private readonly PayPalCheckoutClient _paypal;

        public PayPalPaymentProvider(AppDbContext db, IConfiguration config, PayPalOptions opt, PayPalCheckoutClient paypal)
        {
            _db = db;
            _config = config;
            _opt = opt;
            _paypal = paypal;
        }

        public async Task<string> CreateCheckoutAsync(Guid orderId)
        {
            var order = await _db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) throw new ArgumentException("Order not found.");

            if (order.PaymentStatus == "Paid")
                throw new InvalidOperationException("Order already paid.");

            if (!_opt.IsConfigured)
                throw new NotSupportedException("PayPal is not configured. Add PayPal settings first.");

            var clientBaseUrl = _config["ClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
                throw new InvalidOperationException("Missing config: ClientBaseUrl");

            // Create DB transaction row (same pattern as Stripe)
            var tx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = "PayPal",
                Status = "Created",
                AmountOre = order.TotalOre,
                Currency = order.Currency,
                CreatedUtc = DateTime.UtcNow
            };
            _db.PaymentTransactions.Add(tx);

            order.PaymentProvider = "PayPal";
            order.PaymentStatus = "Pending";
            order.PaymentReference = tx.Id.ToString(); // temporary until we store PayPal order id later
            await _db.SaveChangesAsync();

            // Convert øre -> major units (NOK)
            var amount = order.TotalOre / 100.0m;

            var approvalUrl = await _paypal.CreateOrderAndGetApprovalUrlAsync(
                currency: order.Currency,
                amount: amount,
                returnUrl: $"{clientBaseUrl}/payment/success?orderId={order.Id}&provider=paypal",
                cancelUrl: $"{clientBaseUrl}/payment/cancel?orderId={order.Id}&provider=paypal",
                brandName: _opt.BrandName,
                metadata: new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["transactionId"] = tx.Id.ToString()
                });

            tx.Status = "Pending";
            tx.UpdatedUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return approvalUrl;
        }
    }
}
