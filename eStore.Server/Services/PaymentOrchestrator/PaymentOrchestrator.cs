using eStore.Shared.Enums;

namespace eStore.Server.Services.PaymentOrchestrator
{
    public class PaymentOrchestrator : IPaymentOrchestrator
    {
        private readonly IReadOnlyDictionary<PaymentProvider, IPaymentProvider> _map;

        public PaymentOrchestrator(IEnumerable<IPaymentProvider> providers)
            => _map = providers.ToDictionary(p => p.Provider, p => p);

        public Task<string> CreateCheckoutAsync(Guid orderId, PaymentProvider provider)
        {
            if (!_map.TryGetValue(provider, out var p))
                throw new NotSupportedException($"Payment provider '{provider}' is not enabled.");

            return p.CreateCheckoutAsync(orderId);
        }
    }
}
