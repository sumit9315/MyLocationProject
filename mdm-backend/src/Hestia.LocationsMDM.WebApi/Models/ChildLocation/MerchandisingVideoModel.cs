using System;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class MerchandisingVideoModel
    {
        /// <summary>
        /// Gets or sets the item unique identifier.
        /// </summary>
        public string ItemGuid { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the date/time when video starts.
        /// </summary>
        public DateTime? StartsOn { get; set; }

        /// <summary>
        /// Gets or sets the date/time when video ends.
        /// </summary>
        public DateTime? EndsOn { get; set; }

        /// <summary>
        /// Gets or sets the sequence.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the group sequence.
        /// </summary>
        public int GroupSequence { get; set; }
    }
}
