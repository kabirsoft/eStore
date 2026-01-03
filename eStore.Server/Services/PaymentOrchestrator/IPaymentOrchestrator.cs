using eStore.Shared.Enums;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public interface IPaymentOrchestrator
    {
        Task<string> CreateCheckoutAsync(Guid orderId, PaymentProvider provider);
    }
}
