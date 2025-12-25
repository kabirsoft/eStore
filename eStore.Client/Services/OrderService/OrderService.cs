using eStore.Shared.Dtos;
using System.Net.Http.Json;

namespace eStore.Client.Services.OrderService
{
    public class OrderService : IOrderService
    {
        private readonly HttpClient _http;

        public OrderService(HttpClient http)
        {
            _http = http;
        }

        public async Task<OrderDto> CreateOrderAsync(OrderCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/orders", dto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            var created = await response.Content.ReadFromJsonAsync<OrderDto>();
            return created ?? throw new Exception("Create order failed: empty response.");
        }

        public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
        }
    }
}
