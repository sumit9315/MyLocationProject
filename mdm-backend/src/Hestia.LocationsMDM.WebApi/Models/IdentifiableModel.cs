namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// Base model for all Models with Id.
    /// </summary>
    public abstract class IdentifiableModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; set; }
    }
}
