using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// An model that represents change history search criteria.
    /// </summary>
    public class ChangeHistorySearchCriteria : SearchCriteria
    {
        public string ObjectType { get; set; }
        public string CampusId { get; set; }
        public string RegionNodeId { get; set; }
        public string ChildLocationId { get; set; }
        public string RegionId { get; set; }
        public string State { get; set; }
        public string CityName { get; set; }

        public bool? Hierarchy { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The attribute names to retrieve.
        /// </summary>
        public IList<string> AttributeNames { get; set; }

        [JsonIgnore]
        public IList<string> RegionNodeIds { get; set; }

        [JsonIgnore]
        public IList<string> ChildLocationIds { get; set; }
    }
}
