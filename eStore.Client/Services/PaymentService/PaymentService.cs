using eStore.Shared.Dtos;
using System.Net.Http.Json;

namespace eStore.Client.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _http;

        public PaymentService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> CreateStripeCheckoutUrlAsync(Guid orderId)
        {
            var resp = await _http.PostAsJsonAsync("api/payments/stripe/create-checkout-session",
                new StartStripeCheckoutDto { OrderId = orderId });

            if (!resp.IsSuccessStatusCode)
                throw new Exception(await resp.Content.ReadAsStringAsync());

            var dto = await resp.Content.ReadFromJsonAsync<StripeCheckoutSessionDto>();
            return dto?.Url ?? throw new Exception("Stripe URL missing.");
        }
    }
}
