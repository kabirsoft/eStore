using eStore.Server.Data;
using eStore.Server.Services.OrderService;
using eStore.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eStore.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orders;
        private readonly AppDbContext _db;

        public OrdersController(IOrderService orders, AppDbContext db)
        {
            _orders = orders;
            _db = db;
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create(OrderCreateDto dto)
        {
            try
            {
                var created = await _orders.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (OverflowException)
            {
                return BadRequest("Total amount is too large.");
            }
        }

        // GET: api/orders/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderDto>> GetById(Guid id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is null) return NotFound();
            return Ok(order);
        }

        // GET: api/orders/{id}/payment-result
        [HttpGet("{id:guid}/payment-result")]
        public async Task<ActionResult<PaymentResultDto>> GetPaymentResult(Guid id)
        {
            var result = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Product)
                .Where(o => o.Id == id)
                .Select(o => new PaymentResultDto
                {
                    ProductName = o.Product.Name,
                    Quantity = o.Quantity,
                    OrderId = o.Id,
                    PaymentProvider = o.PaymentProvider ?? "None",
                    PaymentStatus = o.PaymentStatus ?? "Unknown",
                    PaymentReference = o.PaymentReference,
                    OrderStatus = o.Status ?? "Unknown",
                    PaidUtc = o.PaidUtc,
                    AmountOre = o.TotalOre,
                    Currency = o.Currency ?? "NOK"
                })
                .FirstOrDefaultAsync();

            if (result is null) return NotFound();
            return Ok(result);
        }
    }
}
