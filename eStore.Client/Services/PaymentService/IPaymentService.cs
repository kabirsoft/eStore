namespace eStore.Client.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<string> CreateStripeCheckoutUrlAsync(Guid orderId);
    }
}
