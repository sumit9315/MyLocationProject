using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The operating hours model for Update API.
    /// </summary>
    public class OperatingHoursUpdateModel
    {
        /// <summary>
        /// Indicates whether to apply to children locations.
        /// </summary>
        public bool ApplyToChildren { get; set; }

        /// <summary>
        /// The updated operating hours.
        /// </summary>
        public IList<OperatingHoursModel> OperatingHours { get; set; }

        /// <summary>
        /// Indicates the pro-pickup hours.
        /// </summary>
        public string ProPickupDuration { get; set; }
    }
}
