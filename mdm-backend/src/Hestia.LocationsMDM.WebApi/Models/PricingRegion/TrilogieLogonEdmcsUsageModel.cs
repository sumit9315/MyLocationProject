using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Trilogie Logon usage in EDMCS.
    /// </summary>
    public class TrilogieLogonEdmcsUsageModel
    {
        public string TrilogieLogon { get; set; }

        public string CampusNode { get; set; }

        public string RegionNode { get; set; }
    }
}
