using eStore.Shared.Dtos;

namespace eStore.Client.Services.ProductService
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(Guid id);
    }
}
