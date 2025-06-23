using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The time interval model.
    /// </summary>
    public class TimeIntervalString
    {
        /// <summary>
        /// The Start Time
        /// </summary>
        [DataMember(Name = "startTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// The End Time
        /// </summary>
        [DataMember(Name = "endTime")]
        public string EndTime { get; set; }
    }
}
