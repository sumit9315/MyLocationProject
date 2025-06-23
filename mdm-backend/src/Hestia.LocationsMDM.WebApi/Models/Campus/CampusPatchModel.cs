using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Campus Patch model.
    /// </summary>
    public class CampusPatchModel
    {
        /// <summary>
        /// Whether the region is open for business.
        /// </summary>
        public bool? OpenForBusinessFlag { get; set; }

        /// <summary>
        /// Whether the region is open for disclosure.
        /// </summary>
        public bool? OpenForDisclosureFlag { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or Sets Phone Numbers
        /// </summary>
        public IList<string> PhoneNumbers { get; set; }

        /// <summary>
        /// The time-zone Identifier.
        /// </summary>
        public string TimeZoneIdentifier { get; set; }
    }
}
