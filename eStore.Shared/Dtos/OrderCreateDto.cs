using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class OrderCreateDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        // customer info (simple for now)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
