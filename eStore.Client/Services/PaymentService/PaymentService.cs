using eStore.Shared.Dtos;
using eStore.Shared.Enums;
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

        public async Task<string> CreateCheckoutUrlAsync(Guid orderId, PaymentProvider provider)
        {
            var resp = await _http.PostAsJsonAsync("api/payments/create",
                new CreatePaymentRequest(orderId, provider));

            if (!resp.IsSuccessStatusCode)
                throw new Exception(await resp.Content.ReadAsStringAsync());

            var dto = await resp.Content.ReadFromJsonAsync<CreatePaymentResponse>();
            return dto?.RedirectUrl ?? throw new Exception("RedirectUrl missing.");
        }
       
        //public Task<string> CreateStripeCheckoutUrlAsync(Guid orderId)
        //    => CreateCheckoutUrlAsync(orderId, PaymentProvider.Stripe);
    }
}
