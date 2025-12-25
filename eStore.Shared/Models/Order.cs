using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Models
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }   // navigation

        public int Quantity { get; set; } = 1;

        public int UnitPriceOre { get; set; }
        public int TotalOre { get; set; }
        public string Currency { get; set; } = "NOK";

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public string Status { get; set; } = "Created"; // later: Paid, Failed, Cancelled

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
