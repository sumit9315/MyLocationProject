using System.Collections.Generic;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Location's Calendar Events model.
    /// </summary>
    public class LocationCalendarEventsModel
    {
        /// <summary>
        /// The Calendar Events
        /// </summary>
        public IList<CalendarEventModel> CalendarEvents { get; set; }

        /// <summary>
        /// The list of unique planned event names that children locations have.
        /// </summary>
        [CosmosIgnore]
        public IList<string> ChildrenPlannedEventNames { get; set; }
    }
}
