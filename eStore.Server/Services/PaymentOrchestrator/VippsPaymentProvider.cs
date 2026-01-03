using eStore.Server.Data;
using eStore.Server.Payments.Vipps;
using eStore.Shared.Enums;
using eStore.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public class VippsPaymentProvider : IPaymentProvider
    {
        public PaymentProvider Provider => PaymentProvider.Vipps;

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly VippsOptions _opt;
        private readonly VippsEpaymentClient _vipps;

        public VippsPaymentProvider(AppDbContext db, IConfiguration config, VippsOptions opt, VippsEpaymentClient vipps)
        {
            _db = db;
            _config = config;
            _opt = opt;
            _vipps = vipps;
        }

        public async Task<string> CreateCheckoutAsync(Guid orderId)
        {
            if (!_opt.IsConfigured)
                throw new NotSupportedException("Vipps is not enabled (missing config).");

            var order = await _db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) throw new ArgumentException("Order not found.");

            if (order.PaymentStatus == "Paid")
                throw new InvalidOperationException("Order already paid.");

            // Use a stable reference that is unique per MSN. OrderId is perfect.
            var reference = order.Id.ToString("N"); // no dashes
            var idempotencyKey = (order.PaymentReference ?? Guid.NewGuid().ToString("N"));

            // Create (or reuse) a transaction row
            var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(x =>
                x.OrderId == order.Id && x.Provider == "Vipps" && x.Status != "Paid");

            if (tx is null)
            {
                tx = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Provider = "Vipps",
                    Status = "Created",
                    AmountOre = order.TotalOre,
                    Currency = order.Currency,
                    CreatedUtc = DateTime.UtcNow
                };
                _db.PaymentTransactions.Add(tx);
            }

            // Update order state
            order.PaymentProvider = "Vipps";
            order.PaymentStatus = "Pending";
            order.PaymentReference = reference; // store Vipps reference here
            tx.ProviderReference = reference;
            tx.Status = "Pending";
            tx.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var clientBaseUrl = _config["ClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(clientBaseUrl))
                throw new InvalidOperationException("Missing config: ClientBaseUrl");

            // Where Vipps will return the user after the flow:
            var returnUrl = $"{clientBaseUrl}/payment/vipps-return?orderId={order.Id}";

            // Optional: prefill phone number in test if you want. Otherwise null.
            string? phone = null;

            var (redirectUrl, _) = await _vipps.CreatePaymentAsync(
                reference: reference,
                amountOre: order.TotalOre,
                currency: order.Currency,
                returnUrl: returnUrl,
                phoneNumberE164DigitsOnly: phone,
                description: order.Product?.Name ?? "Order",
                idempotencyKey: idempotencyKey
            );

            return redirectUrl;
        }
    }
}
