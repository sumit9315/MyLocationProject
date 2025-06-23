using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The details operating hours model.
    /// </summary>
    public class OperatingHoursModel
    {
        /// <summary>
        /// The day of week (Sunday is 0, Saturday is 6) described by this entity.
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the name of the day of week.
        /// </summary>
        /// <value>
        /// The name of the day of week.
        /// </value>
        [JsonIgnore]
        [Obsolete("TODO: Change all usages to Day")]
        public string DayOfWeekName { get; set; }

        /// <summary>
        /// Gets or sets the name of the day of week.
        /// </summary>
        /// <value>
        /// The name of the day of week.
        /// </value>
        [JsonIgnore]
        public string Day { get; set; }

        /// <summary>
        /// Gets or Sets openHour
        /// </summary>
        public string OpenHour { get; set; }

        /// <summary>
        /// Gets or Sets closeHour
        /// </summary>
        public string CloseHour { get; set; }

        /// <summary>
        /// Gets or Sets open after Hour
        /// </summary>
        public string OpenAfterHour { get; set; }

        /// <summary>
        /// Gets or Sets close after Hour
        /// </summary>
        public string CloseAfterHour { get; set; }

        /// <summary>
        /// Gets or Sets open ReceivingHours
        /// </summary>
        public string OpenReceivingHour { get; set; }

        /// <summary>
        /// Gets or Sets open ReceivingHours
        /// </summary>
        public string CloseReceivingHour { get; set; }

        ///// <summary>
        ///// Indicates whether it is open for the indicated day.
        ///// </summary>
        public string OpenCloseFlag { get; set; }
    }
}
