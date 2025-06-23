namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Titled Contact.
    /// </summary>
    public class TitledContact
    {
        /// <summary>
        /// Gets or sets the associate Id.
        /// </summary>
        /// <value>
        /// The associate Id.
        /// </value>
        public string AssociateId { get; set; }

        /// <summary>
        /// The contact name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The contact title.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The Email title.
        /// </summary>
        public string Email { get; set; }
    }
}
