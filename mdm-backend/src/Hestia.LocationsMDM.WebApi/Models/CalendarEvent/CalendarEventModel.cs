using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ApiIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Calendar Event model.
    /// </summary>
    public class CalendarEventModel
    {
        /// <summary>
        /// The event type.
        /// </summary>
        public CalendarEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        public string LocationNode { get; set; }

        /// <summary>
        /// Gets or sets the name of the location.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// The Unique Id of the event.
        /// </summary>
        public string EventGuid { get; set; }

        /// <summary>
        /// The Id of the parent event. This is not null when event is inherited and modified.
        /// </summary>
        public string ParentGuid { get; set; }

        /// <summary>
        /// The Id of the event.
        /// </summary>
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
        /// Gets or sets the event start date time.
        /// </summary>
        /// <value>
        /// The event start date time.
        /// </value>
        [ApiIgnore]
        public DateTime EventStartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the event end date time.
        /// </summary>
        /// <value>
        /// The event end date time.
        /// </value>
        [ApiIgnore]
        public DateTime EventEndDateTime { get; set; }

        /// <summary>
        /// A boolean to indicate if it's a full day event.
        /// </summary>
        public bool IsFullDay { get; set; }

        /// <summary>
        /// Gets or sets the duration of the event.
        /// </summary>
        /// <value>
        /// The duration of the event.
        /// </value>
        public string EventDuration { get; set; }

        /// <summary>
        /// A boolean to indicate if it's a Closure.
        /// </summary>
        public bool Closure { get; set; }

        /// <summary>
        /// Gets or sets the display sequence.
        /// </summary>
        /// <value>
        /// The display sequence.
        /// </value>
        public int DisplaySequence { get; set; }

        /// <summary>
        /// Represents Id of the Calendar Events Mass Update.
        /// </summary>
        public string MassUpdateId { get; set; }

        /// <summary>
        /// Represents Title of the Calendar Events Mass Update.
        /// </summary>
        [CosmosIgnore]
        public string MassUpdateTitle { get; set; }

        /// <summary>
        /// Indicates whether this event is inherited in Regions () Gets or sets a value indicating whether this instance has inherited children.
        /// </summary>
        [CosmosIgnore]
        public bool IsInheritedInRegions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has inherited children.
        /// </summary>
        [CosmosIgnore]
        public bool IsInheritedInChildLocs { get; set; }
    }
}
