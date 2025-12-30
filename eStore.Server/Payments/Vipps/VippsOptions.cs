namespace eStore.Server.Payments.Vipps
{
    public sealed class VippsOptions
    {
        public string BaseUrl { get; set; } = "https://apitest.vipps.no";

        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string SubscriptionKey { get; set; } = "";
        public string MerchantSerialNumber { get; set; } = "";

        // Recommended headers for debugging (especially in production)
        public string SystemName { get; set; } = "eStore";
        public string SystemVersion { get; set; } = "1.0.0";
        public string PluginName { get; set; } = "eStore";
        public string PluginVersion { get; set; } = "1.0.0";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId) &&
            !string.IsNullOrWhiteSpace(ClientSecret) &&
            !string.IsNullOrWhiteSpace(SubscriptionKey) &&
            !string.IsNullOrWhiteSpace(MerchantSerialNumber);
    }
}
