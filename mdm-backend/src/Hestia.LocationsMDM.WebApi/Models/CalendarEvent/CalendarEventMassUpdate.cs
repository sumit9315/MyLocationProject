using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models.CalendarEvent
{
    public class CalendarEventMassUpdate
    {
        /// <summary>
        /// The Id of the Calendar Events Mass Update.
        /// </summary>
        public string MassUpdateId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public CalendarEventMassUpdateFilter Filter { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string LastUpdatedBy { get; set; }

        [JsonProperty("recEffDt")]
        public DateTime? LastUpdatedDate { get; set; }

        [CosmosIgnore]
        public string FilterDescription { get; set; }

        [CosmosIgnore]
        public IList<CalendarEventModel> CalendarEvents { get; set; }
    }
}
