using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class ProductDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int PriceOre { get; set; }
        public string Currency { get; set; } = "NOK";

        public bool IsActive { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
