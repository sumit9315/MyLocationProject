namespace Hestia.LocationsMDM.WebApi.Config
{
    /// <summary>
    /// The AD Security configuration.
    /// </summary>
    public class SecurityConfig
    {
        /// <summary>
        /// Gets or sets the admin group ID.
        /// </summary>
        /// <value>
        /// The admin group ID.
        /// </value>
        public string AdminGroup { get; set; }

        /// <summary>
        /// Gets or sets the user group ID.
        /// </summary>
        /// <value>
        /// The user group ID.
        /// </value>
        public string UserGroup { get; set; }
    }
}
