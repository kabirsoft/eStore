using System;
using System.Collections.Generic;
using System.Text;

namespace eStore.Shared.Enums
{
    public enum PaymentProvider
    {
        Stripe = 1,
        Vipps = 2
    }
    public record CreatePaymentRequest(Guid OrderId, PaymentProvider Provider);
    public record CreatePaymentResponse(string RedirectUrl);
}
