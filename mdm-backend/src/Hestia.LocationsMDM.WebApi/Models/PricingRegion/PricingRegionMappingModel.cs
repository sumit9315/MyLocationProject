using System.Runtime.Serialization;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Pricing Region Mapping model.
    /// </summary>
    public class PricingRegionMappingModel
    {
        public string PricingRegionId { get; set; }

        public string TrilogieLogon { get; set; }

        public string PricingRegion { get; set; }

        [CosmosIgnore]
        public int CampusCount { get; set; }

        [CosmosIgnore]
        public int ChildLocCount { get; set; }
    }
}
