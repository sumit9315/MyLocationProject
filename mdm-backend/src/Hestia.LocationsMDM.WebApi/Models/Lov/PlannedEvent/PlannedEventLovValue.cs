using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Planned Event LOV value.
    /// </summary>
    public class PlannedEventLovValue
    {
        /// <summary>
        /// The name of the event.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Gets or Sets Event Start date.
        /// </summary>
        public string EventStartDay { get; set; }

        /// <summary>
        /// Gets or Sets Event End date.
        /// </summary>
        public string EventEndDay { get; set; }

        /// <summary>
        /// Gets or Sets Event Start time.
        /// </summary>
        public string EventStartTime { get; set; }

        /// <summary>
        /// Gets or Sets Event End time.
        /// </summary>
        public string EventEndTime { get; set; }

        /// <summary>
        /// A boolean to indicate if it's a full day event.
        /// </summary>
        public bool IsFullDay { get; set; }
    }
}
