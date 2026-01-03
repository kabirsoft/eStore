using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class PaymentResultDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
        public string PaymentProvider { get; set; } = "None";
        public string PaymentStatus { get; set; } = "Unknown";
        public string OrderStatus { get; set; } = "Unknown";
        public string? PaymentReference { get; set; }
        public DateTime? PaidUtc { get; set; }
        public int AmountOre { get; set; }
        public string Currency { get; set; } = "NOK";

    }
}
