using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Dtos
{
    public class StripeCheckoutSessionDto
    {
        public string Url { get; set; } = string.Empty; // Stripe hosted checkout URL
    }
}
