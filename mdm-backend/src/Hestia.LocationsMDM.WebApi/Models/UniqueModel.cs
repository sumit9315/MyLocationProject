namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// Base model for all Models with Unique Identifier.
    /// </summary>
    public abstract class UniqueModel
    {
        /// <summary>
        /// Gets or sets the item unique identifier.
        /// </summary>
        public string ItemGuid { get; set; }
    }
}
