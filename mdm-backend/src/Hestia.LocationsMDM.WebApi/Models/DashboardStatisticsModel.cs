namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The dashboard statistics.
    /// </summary>
    public class DashboardStatisticsModel
    {
        /// <summary>
        /// Gets or sets the active campus count.
        /// </summary>
        /// <value>
        /// The active campus count.
        /// </value>
        public int ActiveCampus { get; set; }

        /// <summary>
        /// Gets or sets the districts count.
        /// </summary>
        /// <value>
        /// The districts count.
        /// </value>
        public int Districts { get; set; }

        /// <summary>
        /// Gets or sets the areas count.
        /// </summary>
        /// <value>
        /// The areas count.
        /// </value>
        public int Areas { get; set; }

        /// <summary>
        /// Gets or sets the regions count.
        /// </summary>
        /// <value>
        /// The regions count.
        /// </value>
        public int Regions { get; set; }
    }
}
