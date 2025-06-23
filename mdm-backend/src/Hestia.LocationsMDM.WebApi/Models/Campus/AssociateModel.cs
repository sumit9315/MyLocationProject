namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Associate model details.
    /// </summary>
    public class AssociateModel
    {
        /// <summary>
        /// Gets or sets the associate identifier.
        /// </summary>
        public string AssociateId { get; set; }

        /// <summary>
        /// Gets or sets the first name of the associate.
        /// </summary>
        /// <value>
        /// The first name of the associate.
        /// </value>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the name of the associate middle.
        /// </summary>
        /// <value>
        /// The name of the associate middle.
        /// </value>
        public string MiddleName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the associate.
        /// </summary>
        /// <value>
        /// The last name of the associate.
        /// </value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the associate title.
        /// </summary>
        /// <value>
        /// The associate title.
        /// </value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the associate Email.
        /// </summary>
        /// <value>
        /// The associate Email.
        /// </value>
        public string Email { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var other = obj as AssociateModel;
            return AssociateId == other?.AssociateId;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return AssociateId.GetHashCode();
        }
    }
}
