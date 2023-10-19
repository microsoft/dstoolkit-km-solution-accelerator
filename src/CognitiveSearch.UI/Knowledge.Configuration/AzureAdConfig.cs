namespace Knowledge.Configuration
{
    public record AzureAdConfig
    {
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string[] Scopes { get; set; }
        public string CallbackPath { get; set; }
    }
}
