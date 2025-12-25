using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public int UnitPriceOre { get; set; }
        public int TotalOre { get; set; }
        public string Currency { get; set; } = "NOK";

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public string Status { get; set; } = "Created"; // later: Paid, Failed, Cancelled
        public DateTime CreatedUtc { get; set; }
    }
}
