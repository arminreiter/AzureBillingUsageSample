namespace CodeHollow.Azure.BillingUsageApp
{
    public class UsageValue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public UsageProperties Properties { get; set; }
    }
}
