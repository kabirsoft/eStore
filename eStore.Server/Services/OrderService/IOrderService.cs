using eStore.Shared.Dtos;

namespace eStore.Server.Services.OrderService
{
    public interface IOrderService
    {
        Task<OrderDto> CreateAsync(OrderCreateDto dto);
        Task<OrderDto?> GetByIdAsync(Guid id);
    }
}
