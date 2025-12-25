using eStore.Server.Data;
using eStore.Shared.Dtos;
using eStore.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Services.OrderService
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;

        public OrderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OrderDto> CreateAsync(OrderCreateDto dto)
        {
            if (dto.ProductId == Guid.Empty)
                throw new ArgumentException("ProductId is required.");

            if (dto.Quantity <= 0)
                throw new ArgumentException("Quantity must be >= 1.");

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                throw new ArgumentException("CustomerName is required.");

            if (string.IsNullOrWhiteSpace(dto.CustomerEmail))
                throw new ArgumentException("CustomerEmail is required.");

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.IsActive);

            if (product is null)
                throw new ArgumentException("Product not found or inactive.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = dto.Quantity,
                UnitPriceOre = product.PriceOre,
                TotalOre = checked(product.PriceOre * dto.Quantity),
                Currency = product.Currency,
                CustomerName = dto.CustomerName.Trim(),
                CustomerEmail = dto.CustomerEmail.Trim(),
                Status = "Created",
                CreatedUtc = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return new OrderDto
            {
                Id = order.Id,
                ProductId = order.ProductId,
                ProductName = product.Name,
                Quantity = order.Quantity,
                UnitPriceOre = order.UnitPriceOre,
                TotalOre = order.TotalOre,
                Currency = order.Currency,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                Status = order.Status,
                CreatedUtc = order.CreatedUtc
            };
        }

        public async Task<OrderDto?> GetByIdAsync(Guid id)
        {
            var o = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (o is null) return null;

            return new OrderDto
            {
                Id = o.Id,
                ProductId = o.ProductId,
                ProductName = o.Product?.Name ?? "",
                Quantity = o.Quantity,
                UnitPriceOre = o.UnitPriceOre,
                TotalOre = o.TotalOre,
                Currency = o.Currency,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                Status = o.Status,
                CreatedUtc = o.CreatedUtc
            };
        }
    }
}
