using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// An model that represents locations search criteria.
    /// </summary>
    public class LocationSearchCriteria : SearchCriteria
    {
        public string Location { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string CountryName { get; set; }
        public string ZipCode { get; set; }
        public string ZipCodeExtn { get; set; }
        public string Region { get; set; }
        public string LobCc { get; set; }
        public string CostCenterId { get; set; }
        public string District { get; set; }
        public string PricingRegion { get; set; }
        public string LocationType { get; set; }
        public string Kob { get; set; }
        public string Glbu { get; set; }

        public string AssociateName { get; set; }

        public string LocationNodes { get; set; }
        public string CostCenterIds { get; set; }

        public bool IncludeCampus { get; set; }
        public bool IncludeRegion { get; set; }
    }
}
