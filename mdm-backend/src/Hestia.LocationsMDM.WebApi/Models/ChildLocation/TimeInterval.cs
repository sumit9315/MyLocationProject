using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The time interval model.
    /// </summary>
    public class TimeInterval
    {
        /// <summary>
        /// The Start Time
        /// </summary>
        [DataMember(Name = "startTime")]
        public TimeOfDay StartTime { get; set; }

        /// <summary>
        /// The End Time
        /// </summary>
        [DataMember(Name = "endTime")]
        public TimeOfDay EndTime { get; set; }
    }
}
