using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Models
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Store money as integer "øre" (NOK * 100) to avoid decimal bugs.
        public int PriceOre { get; set; }

        public string Currency { get; set; } = "NOK";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }
    }
}
