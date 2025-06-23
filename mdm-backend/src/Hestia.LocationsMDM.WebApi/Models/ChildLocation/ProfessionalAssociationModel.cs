using System;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class ProfessionalAssociationModel : UniqueModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Logo source.
        /// </summary>
        public string LogoSource { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the sequence.
        /// </summary>
        public int Sequence { get; set; }
    }
}
