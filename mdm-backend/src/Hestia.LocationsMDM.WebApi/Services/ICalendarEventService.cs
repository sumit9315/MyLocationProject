using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Calendar Event service interface.
    /// </summary>
    public interface ICalendarEventService
    {
        /// <summary>
        /// Searches the calendar events matching given criteria.
        /// </summary>
        /// <param name="sortBy">The sort by criteria.</param>
        /// <param name="eventName">Name of the event criteria.</param>
        /// <param name="eventStartDay">The event start day criteria.</param>
        /// <param name="eventEndDay">The event end day criteria.</param>
        /// <param name="eventType">Type of the event criteria.</param>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>
        /// The macthed calendar events.
        /// </returns>
        Task<SearchResult<CalendarEventModel>> SearchAsync(
            string sortBy,
            string eventName,
            string eventStartDay,
            string eventEndDay,
            CalendarEventType? eventType,
            int pageNum,
            int pageSize);

        /// <summary>
        /// Gets the calendar event by Id.
        /// </summary>
        /// <param name="eventGuid">The calendar event Id.</param>
        /// <returns>The calendar event details.</returns>
        Task<CalendarEventModel> GetAsync(string eventGuid);

        /// <summary>
        /// Gets the next events, taking first 'count' events from now.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="count">The count to take.</param>
        /// <returns>Next events.</returns>
        Task<IList<CalendarEventModel>> GetNextAsync(CalendarEventType eventType, int count);

        /// <summary>
        /// Creates the Calendar Event.
        /// </summary>
        /// <param name="model">The Calendar Event data.</param>
        /// <returns>Created Calendar Event model.</returns>
        Task<JObject> CreateAsync(CalendarEventModel model);

        /// <summary>
        /// Updates the Calendar Event.
        /// </summary>
        /// <param name="eventGuid">The calendar event Giud.</param>
        /// <param name="model">The updated Calendar Event  data.</param>
        /// <returns>Updated object.</returns>
        Task<JObject> UpdateAsync(string eventGuid, CalendarEventPatchModel model);

        /// <summary>
        /// Deletes the Calendar Event by Id.
        /// </summary>
        /// <param name="eventGuid">The calendar event Giud.</param>
        /// <returns>The deleted object.</returns>
        Task<JObject> DeleteAsync(string eventGuid);

        /// <summary>
        /// Deletes the location events.
        /// </summary>
        /// <param name="locationNode">The location node.</param>
        Task DeleteLocationEventsAsync(string locationNode);

        /// <summary>
        /// Gets the list of planned events.
        /// </summary>
        /// <param name="excludePastEvents">Flag indicating whether past events should be excluded.</param>
        /// <returns>
        /// The planned events.
        /// </returns>
        Task<IList<PlannedEventTemplateModel>> GetPlannedEventTemplatesAsync(bool excludePastEvents = false);

        /// <summary>
        /// Gets the location event Guids.
        /// </summary>
        /// <param name="locationNode">The location node.</param>
        /// <returns>The location event Guids.</returns>
        Task<IList<string>> GetLocationEventGuidsAsync(string locationNode);

        /// <summary>
        /// Gets the event Ids by given event Guids.
        /// </summary>
        /// <param name="eventGuids">The event guids.</param>
        /// <returns>The event Ids.</returns>
        Task<IList<string>> GetEventIdsAsync(IList<string> eventGuids);

        #region Mass Update

        /// <summary>
        /// Gets all Calendar Event Mass Updates.
        /// </summary>
        /// <returns>All Calendar Event Mass Updates.</returns>
        Task<SearchResult<CalendarEventMassUpdate>> SearchMassUpdatesAsync(SearchCriteria criteria);

        /// <summary>
        /// Gets Mass Update details.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        /// <returns>The Mass Update details.</returns>
        Task<CalendarEventMassUpdate> GetMassUpdateAsync(string massUpdateId);

        /// <summary>
        /// Creates new Calendar Event Mass Update.
        /// </summary>
        /// <param name="massUpdate">Mass update details.</param>
        /// <returns>Created Mass Update.</returns>
        Task<CalendarEventMassUpdate> CreateMassUpdateAsync(CalendarEventMassUpdate massUpdate);

        /// <summary>
        /// Updates given Mass Update.
        /// </summary>
        /// <param name="massUpdate">The Mass Update details.</param>
        Task UpdateMassUpdateAsync(CalendarEventMassUpdate massUpdate);

        /// <summary>
        /// Deletes Mass Update with the given Id.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        Task DeleteMassUpdateAsync(string massUpdateId);

        #endregion
    }
}