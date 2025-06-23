using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Planned Event Template.
    /// </summary>
    public class PlannedEventTemplateModel
    {
        /// <summary>
        /// Gets or sets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        public string EventId { get; set; }

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

        /// <summary>
        /// Gets or sets the display sequence.
        /// </summary>
        /// <value>
        /// The display sequence.
        /// </value>
        public int DisplaySequence { get; set; }
    }
}
