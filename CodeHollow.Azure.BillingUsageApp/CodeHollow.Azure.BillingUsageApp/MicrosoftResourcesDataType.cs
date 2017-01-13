using System.Collections.Generic;

namespace CodeHollow.Azure.BillingUsageApp
{
    public class MicrosoftResourcesDataType
    {
        public string ResourceUri { get; set; }
        public IDictionary<string, string> Tags { get; set; }
        public IDictionary<string, string> AdditionalInfo { get; set; }
        public string Location { get; set; }
        public string PartNumber { get; set; }
        public string OrderNumber { get; set; }
    }
}
