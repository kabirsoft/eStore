using eStore.Shared.Dtos;
using System.Net.Http.Json;

namespace eStore.Client.Services.ProductService
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _http;

        public ProductService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            return await _http.GetFromJsonAsync<List<ProductDto>>("api/products")
                   ?? new List<ProductDto>();
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            return await _http.GetFromJsonAsync<ProductDto>($"api/products/{id}");
        }
    }
}
