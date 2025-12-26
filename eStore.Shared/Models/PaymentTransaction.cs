using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Models
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        public string Provider { get; set; } = "None";     // Stripe, Vipps
        public string Status { get; set; } = "Created";    // Created, Pending, Succeeded, Failed

        public int AmountOre { get; set; }
        public string Currency { get; set; } = "NOK";

        public string? ProviderReference { get; set; }     // Stripe session id, Vipps reference, etc.
        public string? ErrorMessage { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }
    }
}
