using eStore.Shared.Enums;

namespace eStore.Client.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutUrlAsync(Guid orderId, PaymentProvider provider);
        //Task<string> CreateStripeCheckoutUrlAsync(Guid orderId);
    }
}
