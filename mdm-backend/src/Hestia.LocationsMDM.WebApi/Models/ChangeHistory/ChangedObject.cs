using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Changed Object model.
    /// </summary>
    public class ChangedObject
    {
        /// <summary>
        /// Gets or sets the old object.
        /// </summary>
        /// <value>
        /// The old object.
        /// </value>
        public JObject OldObject { get; set; }

        /// <summary>
        /// Gets or sets the updated object.
        /// </summary>
        /// <value>
        /// The new object.
        /// </value>
        public JObject NewObject { get; set; }
    }
}
