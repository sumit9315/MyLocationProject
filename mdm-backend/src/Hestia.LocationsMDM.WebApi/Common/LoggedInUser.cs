using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Common
{
    /// <summary>
    /// Represents the logged in User.
    /// </summary>
    public class LoggedInUser
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the user role.
        /// </summary>
        /// <value>
        /// The user role.
        /// </value>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the user roles.
        /// </summary>
        /// <value>
        /// The user roles.
        /// </value>
        public IList<string> Roles { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>
        /// The user id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user given name.
        /// </summary>
        /// <value>
        /// The user given name.
        /// </value>
        public string GivenName { get; set; }

        /// <summary>
        /// Gets or sets the user family name.
        /// </summary>
        /// <value>
        /// The user family name.
        /// </value>
        public string FamilyName { get; set; }

        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        /// <value>
        /// The user email.
        /// </value>
        public string Email { get; set; }
    }
}
