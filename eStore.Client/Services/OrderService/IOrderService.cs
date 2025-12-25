using eStore.Shared.Dtos;

namespace eStore.Client.Services.OrderService
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(OrderCreateDto dto);
        Task<OrderDto?> GetOrderByIdAsync(Guid id);
    }
}
