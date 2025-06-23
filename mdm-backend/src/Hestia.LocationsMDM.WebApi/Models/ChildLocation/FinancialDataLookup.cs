using System;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class FinancialDataLookup
    {
        /// <summary>
        /// The child location cost center id
        /// </summary>
        public string CostCenterId { get; set; }

        /// <summary>
        /// The lob cc.
        /// </summary>
        public string LobCc { get; set; }
    }
}
