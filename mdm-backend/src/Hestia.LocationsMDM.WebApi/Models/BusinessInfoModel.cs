using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Business Info model.
    /// </summary>
    public class BusinessInfoModel
    {
        /// <summary>
        /// Indicates whether the location is open for business.
        /// </summary>
        public bool? OpenForBusiness { get; set; }

        /// <summary>
        /// Indicates whether the location is open for disclosure. Only present if the caller is  Admin-role user. 
        /// </summary>
        public bool? OpenForDisclosure { get; set; }
    }
}
