using eStore.Shared.Dtos;

namespace eStore.Server.Services.ProductService
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<ProductDto> CreateAsync(ProductCreateDto dto);
        Task<ProductDto?> UpdateAsync(Guid id, ProductUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
