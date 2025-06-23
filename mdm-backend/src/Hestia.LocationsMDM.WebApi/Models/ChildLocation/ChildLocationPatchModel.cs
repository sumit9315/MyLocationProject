using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Child Location Patch model.
    /// </summary>
    public class ChildLocationPatchModel
    {
        /// <summary>
        /// The name of the location.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// The node address.
        /// </summary>
        public NodeAddress Address { get; set; }

        /// <summary>
        /// The DBA name.
        /// </summary>
        public string DbaName { get; set; }

        /// <summary>
        /// The product offerings.
        /// </summary>
        public List<string> ProductOffering { get; set; }

        /// <summary>
        /// The longitude.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// The latitude.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// The Phone Numbers.
        /// </summary>
        public IList<string> PhoneNumbers { get; set; }

        /// <summary>
        /// The client should send the current 'LastUpdatedTimestamp' value when making a request to update the location fields.  This will allow the server to ensure that the location object has not been changed (by a different user) prior to the write. 
        /// </summary>
        public DateTime? LastUpdatedTimestamp { get; set; }

        /// <summary>
        /// The updated Region Node Ed.
        /// </summary>
        public string RegionNodeId { get; set; }

        /// <summary>
        /// Gets or sets the updated Services and Certification.
        /// </summary>
        public IList<string> ServicesCertification { get; set; }

        /// <summary>
        /// Gets or sets the updated Value Added Services.
        /// </summary>
        public IList<string> ValueAddedServices { get; set; }

        /// <summary>
        /// The updated Location Landing Page URL.
        /// </summary>
        public string LocationLandingPageURL { get; set; }

        /// <summary>
        /// The updated Dom Location Status.
        /// </summary>
        public string DomLocationStatus { get; set; }

        /// <summary>
        /// The updated Customer Facing Location Name.
        /// </summary>
        public string CustomerFacingLocationName { get; set; }

        /// <summary>
        /// Gets or sets the vanity KOB phone number.
        /// </summary>
        /// <value>
        /// The vanity KOB phone number.
        /// </value>
        public string VanityKobPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the vanity KOB Fax number.
        /// </summary>
        /// <value>
        /// The vanity KOB Fax number.
        /// </value>
        public string VanityKobFaxNumber { get; set; }

        /// <summary>
        /// Gets or sets the main branch number.
        /// </summary>
        public string MainBranchNumber { get; set; }

        /// <summary>
        /// Gets or sets the text to counter phone number.
        /// </summary>
        public string TextToCounterPhoneNumber { get; set; }
    }
}
