using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class LocationDoc
    {
        public string Id { get; set; }
        public string CampusNodeId { get; set; }
        public string RegionNodeId { get; set; }

        public string LocationId { get; set; }
        public string LocationName { get; set; }

        public string Name { get; set; }

        public string AreaId { get; set; }
        public string AreaName { get; set; }

        public string RegionId { get; set; }
        public string RegionName { get; set; }

        public string DistrictId { get; set; }
        public string DistrictName { get; set; }

        public string LobId { get; set; }
        public string LobDescription { get; set; }
        public string LobCc { get; set; }
        public string LobCcName { get; set; }

        public IList<FinancialDataItem> financialData { get; set; }

        public NodeAddress Address { get; set; }

        public string LocationType { get; set; }

        public string InventoryOrg { get; set; }
    }
}
