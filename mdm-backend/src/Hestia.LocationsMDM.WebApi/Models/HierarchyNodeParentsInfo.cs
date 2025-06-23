using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Hierarchy Node parents info.
    /// </summary>
    public class HierarchyNodeParentsInfo
    {
        public string CampusId { get; set; }

        public string RegionId { get; set; }

        public string State { get; set; }

        public string CityName { get; set; }
    }
}
