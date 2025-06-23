using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Region Patch model.
    /// </summary>
    public class RegionPatchModel
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
        /// Gets or Sets Phone Numbers
        /// </summary>
        public IList<string> PhoneNumbers { get; set; }
    }
}
