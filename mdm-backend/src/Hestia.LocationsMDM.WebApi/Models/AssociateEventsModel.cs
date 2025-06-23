using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Associate Events model.
    /// </summary>
    public class AssociateEventsModel
    {
        /// <summary>
        /// Indicates whether to apply to children locations.
        /// </summary>
        public bool ApplyToChildren { get; set; }

        /// <summary>
        /// Gets or Sets Events Ids to associate.
        /// </summary>
        public IList<string> EventIds { get; set; }
    }
}
