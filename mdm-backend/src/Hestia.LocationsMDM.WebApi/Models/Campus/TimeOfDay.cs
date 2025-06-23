using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Time of the Day details.
    /// </summary>
    public class TimeOfDay
    {
        /// <summary>
        /// The hour component of this daily time (0-23).
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// The minute component of this daily time (0-59).
        /// </summary>
        public int Minute { get; set; }
    }
}
