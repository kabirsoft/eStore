using eStore.Shared.Dtos;

namespace eStore.Client.Services.ProductService
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<ProductDto> CreateProductAsync(ProductCreateDto dto);
        Task<ProductDto?> UpdateProductAsync(Guid id, ProductUpdateDto dto);
        Task<bool> DeleteProductAsync(Guid id);


    }
}
