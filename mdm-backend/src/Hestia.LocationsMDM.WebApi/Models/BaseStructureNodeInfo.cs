namespace Hestia.LocationsMDM.WebApi.Models
{
    public class BaseStructureNodeInfo
    {
        public string CampusId { get; set; }
        public string CampusName { get; set; }

        public string RegionId { get; set; }
        public string RegionName { get; set; }

        public string ChildLocId { get; set; }
        public string ChildLocName { get; set; }
    }
}
