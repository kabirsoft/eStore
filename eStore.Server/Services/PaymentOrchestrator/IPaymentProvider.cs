using eStore.Shared.Enums;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public interface IPaymentProvider
    {
        PaymentProvider Provider { get; }
        Task<string> CreateCheckoutAsync(Guid orderId);
    }
}
