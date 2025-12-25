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

        public async Task<ProductDto> CreateProductAsync(ProductCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/products", dto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            var created = await response.Content.ReadFromJsonAsync<ProductDto>();
            return created ?? throw new Exception("Create failed: empty response.");
        }

        public async Task<ProductDto?> UpdateProductAsync(Guid id, ProductUpdateDto dto)
        {
            var response = await _http.PutAsJsonAsync($"api/products/{id}", dto);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var response = await _http.DeleteAsync($"api/products/{id}");
            return response.IsSuccessStatusCode;
        }

    }
}
