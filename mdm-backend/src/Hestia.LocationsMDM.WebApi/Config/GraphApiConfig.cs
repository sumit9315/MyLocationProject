namespace Hestia.LocationsMDM.WebApi.Config
{
    /// <summary>
    /// The Graph API configuration.
    /// </summary>
    public class GraphApiConfig
    {
        /// <summary>
        /// Gets or sets the base url.
        /// </summary>
        /// <value>
        /// The base url.
        /// </value>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the API scopes.
        /// </summary>
        /// <value>
        /// The API scopes.
        /// </value>
        public string Scopes { get; set; }

        /// <summary>
        /// Gets or sets the default scope URL.
        /// </summary>
        /// <value>
        /// The default scope URL.
        /// </value>
        public string DefaultScope { get; set; }

        /// <summary>
        /// Gets or sets the cache expiration time.
        /// </summary>
        /// <value>
        /// The cache expiration time.
        /// </value>
        public int CacheExpiration { get; set; }

        /// <summary>
        /// Gets or sets the use graph API flag.
        /// </summary>
        /// <value>
        /// The use graph API flag.
        /// </value>
        public bool UseGraphApi { get; set; }
    }
}
