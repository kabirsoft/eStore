namespace eStore.Server.Payments.PayPal
{
    public class PayPalOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";
        public string BrandName { get; set; } = "eStore";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId) &&
            !string.IsNullOrWhiteSpace(ClientSecret) &&
            !string.IsNullOrWhiteSpace(BaseUrl);
    }
}
