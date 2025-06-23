using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Trilogie Logon usage model.
    /// </summary>
    public class TrilogieLogonUsageModel
    {
        public string TrilogieLogon { get; set; }

        public int CampusCount { get; set; }

        public int ChildLocCount { get; set; }
    }
}
