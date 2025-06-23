using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Change History model.
    /// </summary>
    public class ChangeHistoryModel
    {
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
        /// Gets or sets the last changed date.
        /// </summary>
        /// <value>
        /// The last changed date.
        /// </value>
        public DateTime LastChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the last changed attributes.
        /// </summary>
        /// <value>
        /// The last changed attributes.
        /// </value>
        public IList<AttributeChangeModel> LastChangedAttributes { get; set; }
    }
}
