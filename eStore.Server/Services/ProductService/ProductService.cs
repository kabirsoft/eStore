using eStore.Server.Data;
using eStore.Shared.Dtos;
using Microsoft.EntityFrameworkCore;


namespace eStore.Server.Services.ProductService
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;

        public ProductService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedUtc)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    PriceOre = p.PriceOre,
                    Currency = p.Currency,
                    IsActive = p.IsActive,
                    CreatedUtc = p.CreatedUtc,
                    UpdatedUtc = p.UpdatedUtc
                })
                .ToListAsync();
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return null;

            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PriceOre = p.PriceOre,
                Currency = p.Currency,
                IsActive = p.IsActive,
                CreatedUtc = p.CreatedUtc,
                UpdatedUtc = p.UpdatedUtc
            };
        }

        public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required.");

            if (dto.PriceOre < 0)
                throw new ArgumentException("PriceOre must be >= 0.");

            var currency = string.IsNullOrWhiteSpace(dto.Currency) ? "NOK" : dto.Currency.Trim().ToUpperInvariant();

            var product = new eStore.Shared.Models.Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                PriceOre = dto.PriceOre,
                Currency = currency,
                IsActive = dto.IsActive,
                CreatedUtc = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                PriceOre = product.PriceOre,
                Currency = product.Currency,
                IsActive = product.IsActive,
                CreatedUtc = product.CreatedUtc,
                UpdatedUtc = product.UpdatedUtc
            };
        }

        public async Task<ProductDto?> UpdateAsync(Guid id, ProductUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required.");

            if (dto.PriceOre < 0)
                throw new ArgumentException("PriceOre must be >= 0.");

            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product is null) return null;

            product.Name = dto.Name.Trim();
            product.Description = dto.Description;
            product.PriceOre = dto.PriceOre;

            if (!string.IsNullOrWhiteSpace(dto.Currency))
                product.Currency = dto.Currency.Trim().ToUpperInvariant();

            product.IsActive = dto.IsActive;
            product.UpdatedUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                PriceOre = product.PriceOre,
                Currency = product.Currency,
                IsActive = product.IsActive,
                CreatedUtc = product.CreatedUtc,
                UpdatedUtc = product.UpdatedUtc
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product is null) return false;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
