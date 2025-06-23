using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ApiIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Child Location summary model.
    /// </summary>
    public class ChildLocationSummaryModel
    {
        /// <summary>
        /// The child location node Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The child location node Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The child location address
        /// </summary>
        public NodeAddress Address { get; set; }

        /// <summary>
        /// Gets or Sets FinancialData
        /// </summary>
        public IList<FinancialDataItem> FinancialData { get; set; }

        /// <summary>
        /// The district name of the location.
        /// </summary>
        public string DistrictName { get; set; }

        /// <summary>
        /// The region name of the location.
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// The type of the location.
        /// </summary>
        public string LocationType { get; set; }

        /// <summary>
        /// The partition key.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the KOB.
        /// </summary>
        /// <value>
        /// The KOB.
        /// </value>
        public string KOB { get; set; }

        /// <summary>
        /// Gets or sets the pricing region dynamic.
        /// </summary>
        /// <value>
        /// The pricing region dynamic.
        /// </value>
        [ApiIgnore]
        public IList<string> PricingRegions { get; set; }

        /// <summary>
        /// The pricing region.
        /// </summary>
        public string PricingRegion { get; set; }

        /// <summary>
        /// Gets or sets the associate Ids.
        /// </summary>
        /// <value>
        /// The associate Ids.
        /// </value>
        [JsonIgnore]
        public IList<AssociateModel> Associate { get; set; }

        /// <summary>
        /// Gets or Sets Contacts
        /// </summary>
        public IList<TitledContact> Contacts { get; set; }
    }
}
