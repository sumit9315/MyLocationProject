using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Child Location details model.
    /// </summary>
    public class ChildLocationDetailsModel
    {
        /// <summary>
        /// The child location node identifier.
        /// </summary>
        public string Node { get; set; }

        /// <summary>
        /// The child location DBA name
        /// </summary>
        public string DbaName { get; set; }

        /// <summary>
        /// The child location address
        /// </summary>
        public NodeAddress Address { get; set; }

        /// <summary>
        /// Gets or Sets FinancialData
        /// </summary>
        public IList<FinancialDataItem> FinancialData { get; set; }

        /// <summary>
        /// The Product Offering
        /// </summary>
        public IList<string> ProductOffering { get; set; }

        /// <summary>
        /// The Brands
        /// </summary>
        public IList<string> Brands { get; set; }

        /// <summary>
        /// The Professional Associations
        /// </summary>
        public IList<ProfessionalAssociationModel> ProfessionalAssociations { get; set; }

        /// <summary>
        /// The merchandising videos.
        /// </summary>
        public IList<MerchandisingVideoModel> MerchandisingVideos { get; set; }

        /// <summary>
        /// The merchandising banners.
        /// </summary>
        public IList<MerchandisingBannerModel> MerchandisingBanners { get; set; }

        /// <summary>
        /// The content of the branch additional.
        /// </summary>
        public ChildBranchAdditionalContentModel BranchAdditionalContent { get; set; }

        /// <summary>
        /// The Value Added Services
        /// </summary>
        public IList<string> ValueAddedServices { get; set; }

        /// <summary>
        /// The Services and Certification
        /// </summary>
        public List<string> ServicesCertification { get; set; }

        /// <summary>
        /// The child location id. This is different from the node id.
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// The child location name.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// The child location type.
        /// </summary>
        public string LocationType { get; set; }

        /// <summary>
        /// The EDMCS location name.
        /// </summary>
        public string EdmcsLocationName { get; set; }

        /// <summary>
        /// The campus location id. This is different from the node id.
        /// </summary>
        public string CampusNodeId { get; set; }

        /// <summary>
        /// The region location id. This is different from the node id.
        /// </summary>
        public string RegionNodeId { get; set; }

        /// <summary>
        /// The landing page url.
        /// </summary>
        public string LandingPageUrl { get; set; }

        /// <summary>
        /// The location phone numbers.
        /// </summary>
        public List<string> LocationPhoneNumbers { get; set; }

        /// <summary>
        /// The customer-facing location name.
        /// </summary>
        public string CustomerFacingLocationName { get; set; }

        /// <summary>
        /// The DOM Locaiton status.
        /// </summary>
        public string DomLocationStatus { get; set; }

        /// <summary>
        /// The latitude.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// The longitude.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Indicates whether to apply daylight savings.
        /// </summary>
        public bool DayLightSavings { get; set; }

        /// <summary>
        /// The Calendar Events
        /// </summary>
        public IList<CalendarEventModel> CalendarEvents { get; set; }

        /// <summary>
        /// Gets or Sets BusinessInfo
        /// </summary>
        public ChildLocBusinessInfoModel BusinessInfo { get; set; }

        /// <summary>
        /// Gets or Sets OperatingHours
        /// </summary>
        public IList<OperatingHoursModel> OperatingHours { get; set; }

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

        /// <summary>
        /// When this location was last updated.
        /// </summary>
        public DateTime? LastUpdatedOn { get; set; }

        /// <summary>
        /// The district name of the location.
        /// </summary>
        public string DistrictName { get; set; }

        /// <summary>
        /// The region id.
        /// </summary>
        public string RegionId { get; set; }

        /// <summary>
        /// The region name of the location.
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// The timezone identifier.
        /// </summary>
        public string TimezoneIdentifier { get; set; }

        /// <summary>
        /// Indicates whether there is pro-pickup hours.
        /// </summary>
        public bool? ProPickup { get; set; }

        /// <summary>
        /// Indicates the pro-pickup hours.
        /// </summary>
        public string ProPickupDuration { get; set; }

        /// <summary>
        /// Gets or sets the KOB.
        /// </summary>
        /// <value>
        /// The KOB.
        /// </value>
        public string Kob { get; set; }

        /// <summary>
        /// Gets or sets the vanity KOB phone number.
        /// </summary>
        /// <value>
        /// The vanity KOB phone number.
        /// </value>
        public string VanityKobPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the vanity KOB Fax number.
        /// </summary>
        /// <value>
        /// The vanity KOB Fax number.
        /// </value>
        public string VanityKobFaxNumber { get; set; }

        /// <summary>
        /// Gets or sets the main branch number.
        /// </summary>
        public string MainBranchNumber { get; set; }

        /// <summary>
        /// Gets or sets a BOPIS flag.
        /// </summary>
        /// <value>
        /// The BOPIS flag.
        /// </value>
        public bool Bopis { get; set; }

        /// <summary>
        /// Gets or sets the Text to Counter flag.
        /// </summary>
        /// <value>
        /// The Text to Counter flag.
        /// </value>
        public bool TextToCounter { get; set; }
    }
}
