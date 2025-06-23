using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Calendar Event patch model.
    /// </summary>
    public class CalendarEventPatchModel
    {
        /// <summary>
        /// The name of the event.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// The description of the event.
        /// </summary>
        public string EventDescription { get; set; }

        /// <summary>
        /// The display duration.
        /// </summary>
        public int DisplayDuration { get; set; }

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

        /// <summary>
        /// A boolean to indicate if it's a Closure.
        /// </summary>
        public bool Closure { get; set; }

        /// <summary>
        /// Gets or sets the duration of the event.
        /// </summary>
        /// <value>
        /// The duration of the event.
        /// </value>
        public string EventDuration { get; set; }
    }
}
