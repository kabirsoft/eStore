using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class StartStripeCheckoutDto
    {
        public Guid OrderId { get; set; }
    }
}
