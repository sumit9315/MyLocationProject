using System;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class MerchandisingBannerModel : UniqueModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the image.
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Gets or sets the date/time when banner starts.
        /// </summary>
        public DateTime? StartsOn { get; set; }

        /// <summary>
        /// Gets or sets the date/time when banner ends.
        /// </summary>
        public DateTime? EndsOn { get; set; }

        /// <summary>
        /// The long text.
        /// </summary>
        public string LongText { get; set; }

        /// <summary>
        /// The referring cid.
        /// </summary>
        public string ReferringCid { get; set; }

        /// <summary>
        /// The referring domain.
        /// </summary>
        public string ReferringDomain { get; set; }

        /// <summary>
        /// Gets or sets the sequence.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the group sequence.
        /// </summary>
        public int? GroupSequence { get; set; }

        /// <summary>
        /// The time.
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// The supporting document.
        /// </summary>
        public string SupportingDocument { get; set; }
    }
}
