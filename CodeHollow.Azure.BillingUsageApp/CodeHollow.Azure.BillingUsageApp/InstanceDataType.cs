using Newtonsoft.Json;

namespace CodeHollow.Azure.BillingUsageApp
{
    public class InstanceDataType
    {
        [JsonProperty("Microsoft.Resources")]
        public MicrosoftResourcesDataType MicrosoftResources { get; set; }
    }
}
