using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Object Change model.
    /// </summary>
    public class ObjectChangeModel
    {
        /// <summary>
        /// Gets the location identifier.
        /// </summary>
        /// <value>
        /// The location identifier.
        /// </value>
        public string LocationId { get => ObjectId; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>
        /// The name of the location.
        /// </value>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the object identifier.
        /// </summary>
        /// <value>
        /// The object identifier.
        /// </value>
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the name of the city.
        /// </summary>
        /// <value>
        /// The name of the city.
        /// </value>
        public string CityName { get; set; }

        /// <summary>
        /// Gets or sets the changed on date.
        /// </summary>
        /// <value>
        /// The changed on date.
        /// </value>
        public DateTime? ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets the edited by value.
        /// </summary>
        /// <value>
        /// The edit by value.
        /// </value>
        public string EditedBy { get; set; }

        /// <summary>
        /// Gets or sets the changed attributes.
        /// </summary>
        /// <value>
        /// The changed attributes.
        /// </value>
        public AttributeChangeModel ChangedAttribute { get; set; }
    }
}
