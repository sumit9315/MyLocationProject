using System;
using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Business Info update model.
    /// </summary>
    public class BusinessInfoUpdateModel : ChildLocBusinessInfoModel
    {
        /// <summary>
        /// The client should send the current 'lastUpdatedBy' value when making a request to update the location fields.
        /// This will allow the server to ensure that the location object has not been changed (by a different user) prior to the write. 
        /// </summary>
        public DateTime LastUpdatedTimestamp { get; set; }
    }
}
