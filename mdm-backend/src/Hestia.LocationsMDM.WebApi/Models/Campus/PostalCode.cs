namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Postal Code details.
    /// </summary>
    public class PostalCode
    {
        /// <summary>
        /// The primary postal code.
        /// </summary>
        public string Primary { get; set; }

        /// <summary>
        /// The extension postal code.
        /// </summary>
        public string Extension { get; set; }
    }
}
