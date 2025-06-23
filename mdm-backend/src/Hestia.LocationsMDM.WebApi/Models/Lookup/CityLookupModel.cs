namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// City lookup model.
    /// </summary>
    public class CityLookupModel
    {
        /// <summary>
        /// Gets or sets the name of the city.
        /// </summary>
        /// <value>
        /// The name of the city.
        /// </value>
        public string CityName { get; set; }

        /// <summary>
        /// Gets or sets the state code.
        /// </summary>
        /// <value>
        /// The state code.
        /// </value>
        public string StateCode { get; set; }
    }
}
