using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// An model that represents change history search criteria.
    /// </summary>
    public class ChangeSummarySearchCriteria : SearchCriteria
    {
        public string ObjectType { get; set; }
        public string CampusId { get; set; }
        public string RegionId { get; set; }
        public string ChildLocationId { get; set; }

        public bool? Hierarchy { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [JsonIgnore]
        public IList<string> RegionNodeIds { get; set; }

        [JsonIgnore]
        public IList<string> ChildLocationIds { get; set; }
    }
}
