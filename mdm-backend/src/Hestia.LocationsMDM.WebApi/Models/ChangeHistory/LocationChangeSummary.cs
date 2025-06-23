using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Location Change Summary model.
    /// </summary>
    public class LocationChangeSummary
    {
        /// <summary>
        /// Gets the location identifier.
        /// </summary>
        /// <value>
        /// The location identifier.
        /// </value>
        public string LocationId { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the type of the location node.
        /// </summary>
        /// <value>
        /// The type of the location node.
        /// </value>
        public HierarchyNodeType LocationNodeType { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public NodeMainAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the last changed date.
        /// </summary>
        /// <value>
        /// The last changed date.
        /// </value>
        public DateTime LastChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the edited by value.
        /// </summary>
        /// <value>
        /// The edit by value.
        /// </value>
        public string EditedBy { get; set; }
    }
}