using System;
using System.Collections.Generic;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using ApiIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models.CalendarEvent
{
    public class CalendarEventMassUpdateFilter
    {
        public string CityName { get; set; }

        public string State { get; set; }

        public string CountryName { get; set; }

        public string Kob { get; set; }

        public string LocationType { get; set; }

        public string ChildLocNodes { get; set; }

        public string CostCenterIds { get; set; }

        [CosmosIgnore]
        public IList<string> ExcludedLocationNodes { get; set; }

        [ApiIgnore]
        public IList<string> CalendarEventGuids { get; set; }
    }
}
