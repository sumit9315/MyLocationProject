using System.Collections.Generic;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Region details model.
    /// </summary>
    public class RegionDetailsModel : IdentifiableModel
    {
        /// <summary>
        /// The region name.
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// The region id.
        /// </summary>
        public string RegionId { get; set; }

        /// <summary>
        /// Gets or sets the campus node identifier.
        /// </summary>
        /// <value>
        /// The campus node identifier.
        /// </value>
        public string CampusNodeId { get; set; }

        /// <summary>
        /// The pricing regions.
        /// </summary>
        public IList<string> PricingRegion { get; set; }

        /// <summary>
        /// The region address
        /// </summary>
        public NodePrimaryAddress Address { get; set; }

        /// <summary>
        /// The fusion ids.
        /// </summary>
        public IList<string> FusionId { get; set; }

        /// <summary>
        /// The Business Info.
        /// </summary>
        public BusinessInfoModel BusinessInfo { get; set; }

        /// <summary>
        /// Gets or Sets Region Phone Numbers
        /// </summary>
        public IList<string> RegionPhoneNumbers { get; set; }

        /// <summary>
        /// Gets or Sets Location Info
        /// </summary>
        public IList<LocationShort> LocationInfo { get; set; }

        /// <summary>
        /// Gets or Sets Region Contacts
        /// </summary>
        public IList<TitledContact> RegionContacts { get; set; }

        /// <summary>
        /// The Calendar Events
        /// </summary>
        public IList<CalendarEventModel> CalendarEvents { get; set; }

        /// <summary>
        /// The list of unique planned event names that children locations have.
        /// </summary>
        [CosmosIgnore]
        public IList<string> ChildrenPlannedEventNames { get; set; }

        /// <summary>
        /// The location latitude.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// The location longitude.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// The timezone identifier.
        /// </summary>
        public string TimezoneIdentifier { get; set; }

        /// <summary>
        /// Gets or Sets child summary
        /// </summary>
        public IList<HierarchyNode> ChildSummary { get; set; }
    }
}
