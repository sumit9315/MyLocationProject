using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Location Events model.
    /// </summary>
    public class LocationEventsModel
    {
        public IList<CalendarEventModel> PlannedEvents { get; set; }

        public IList<CalendarEventModel> UnplannedEvents { get; set; }
    }
}
