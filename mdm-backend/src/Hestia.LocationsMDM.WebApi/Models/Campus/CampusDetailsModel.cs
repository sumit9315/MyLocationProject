using System.Collections.Generic;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Campus details model.
    /// </summary>
    public class CampusDetailsModel
    {
        /// <summary>
        /// The campus name
        /// </summary>
        public string CampusName { get; set; }

        /// <summary>
        /// The campus id
        /// </summary>
        public string CampusId { get; set; }

        /// <summary>
        /// The campus address
        /// </summary>
        public NodePrimaryAddress Address { get; set; }

        /// <summary>
        /// Gets or Sets Campus Phone Numbers
        /// </summary>
        public IList<string> CampusPhoneNumbers { get; set; }

        /// <summary>
        /// The location latitude.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// The location longitude.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// The pricing regions.
        /// </summary>
        public IList<string> PricingRegion { get; set; }

        /// <summary>
        /// Gets or Sets LocationInfo
        /// </summary>
        public IList<LocationShort> LocationInfo { get; set; }

        /// <summary>
        /// The Business Info.
        /// </summary>
        public BusinessInfoModel BusinessInfo { get; set; }

        /// <summary>
        /// The timezone identifier.
        /// </summary>
        public string TimezoneIdentifier { get; set; }

        /// <summary>
        /// Gets or Sets Campus Contacts
        /// </summary>
        public IList<TitledContact> CampusContacts { get; set; }

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
        /// Gets or Sets child summary
        /// </summary>
        public IList<HierarchyNode> ChildSummary { get; set; }
    }
}
