using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Child Location Create model.
    /// </summary>
    public class ChildLocationCreateModel
    {
        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>
        /// The type of the location.
        /// </value>
        public string LocationType { get; set; }

        /// <summary>
        /// Gets or sets the KOB (Kind of Business).
        /// </summary>
        /// <value>
        /// The KOB (Kind of Business).
        /// </value>
        public string Kob { get; set; }

        /// <summary>
        /// The customer-facing location name.
        /// </summary>
        public string CustomerFacingLocationName { get; set; }

        /// <summary>
        /// The Location ID.
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// The Region Node Id.
        /// </summary>
        public string RegionNodeId { get; set; }
    }
}
