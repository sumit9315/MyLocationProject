using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The model for assigning associates.
    /// </summary>
    public class AssignAssociatesModel
    {
        /// <summary>
        /// Gets or sets the contact list.
        /// </summary>
        /// <value>
        /// The contact list.
        /// </value>
        public IList<string> ContactList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether contacts should be associated with children locations.
        /// </summary>
        /// <value>
        ///   <c>true</c> if contacts should be associated with children locations; otherwise, <c>false</c>.
        /// </value>
        public bool ApplyToChildren { get; set; }
    }
}
