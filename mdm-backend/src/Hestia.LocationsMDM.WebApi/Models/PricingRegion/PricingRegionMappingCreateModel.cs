using System.Runtime.Serialization;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Pricing Region Mapping model for Create/Update operations.
    /// </summary>
    public class PricingRegionMappingCreateModel
    {
        public string TrilogieLogon { get; set; }

        public string PricingRegion { get; set; }
    }
}
