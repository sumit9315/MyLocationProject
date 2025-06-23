using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using System;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using Hestia.LocationsMDM.WebApi.Config;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Calendar Event management controller.
    /// </summary>
    [Route("calendarEvents")]
    public class CalendarEventController : BaseController
    {
        /// <summary>
        /// The Calendar Event service.
        /// </summary>
        private readonly ICalendarEventService _calendarEventService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventController" /> class.
        /// </summary>
        /// <param name="calendarEventService">The calendar event service.</param>
        public CalendarEventController(ICalendarEventService calendarEventService)
        {
            _calendarEventService = calendarEventService;
        }

        /// <summary>
        /// Gets the next events, taking first 'count' events from now.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="count">The count to take.</param>
        /// <returns>Next events.</returns>
        [HttpGet("next/{eventType}")]
        public async Task<IList<CalendarEventModel>> GetAsync(CalendarEventType eventType, int count)
        {
            var result = await _calendarEventService.GetNextAsync(eventType, count);
            return result;
        }

        /// <summary>
        /// Gets the list of planned events.
        /// </summary>
        /// <returns>
        /// The planned events.
        /// </returns>
        [HttpGet("plannedEventTemplates")]
        public async Task<IList<PlannedEventTemplateModel>> GetPlannedEventTemplatesAsync()
        {
            var result = await _calendarEventService.GetPlannedEventTemplatesAsync();
            return result;
        }

        /// <summary>
        /// Gets all Calendar Event Mass Updates.
        /// </summary>
        /// <returns>All Calendar Event Mass Updates.</returns>
        [HttpGet("massUpdates")]
        public async Task<SearchResult<CalendarEventMassUpdate>> SearchMassUpdatesAsync([FromQuery] SearchCriteria criteria)
        {
            criteria ??= new SearchCriteria();
            if (string.IsNullOrEmpty(criteria.SortBy))
            {
                criteria.SortBy = "massUpdateId";
                criteria.SortOrder = SortOrder.Desc;
            }

            var result = await _calendarEventService.SearchMassUpdatesAsync(criteria);
            return result;
        }

        /// <summary>
        /// Gets Mass Update details.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        /// <returns>Calendar Event Mass Update details.</returns>
        [HttpGet("massUpdates/{massUpdateId}")]
        public async Task<CalendarEventMassUpdate> GetMassUpdateAsync(string massUpdateId)
        {
            var result = await _calendarEventService.GetMassUpdateAsync(massUpdateId);
            return result;
        }

        /// <summary>
        /// Creates new Calendar Event Mass Update.
        /// </summary>
        /// <param name="massUpdate">Mass update details.</param>
        /// <returns>Created Mass Update.</returns>
        [HttpPost("massUpdates")]
        public async Task<CalendarEventMassUpdate> CreateMassUpdate(CalendarEventMassUpdate massUpdate)
        {
            var createdItem = await _calendarEventService.CreateMassUpdateAsync(massUpdate);
            return createdItem;
        }

        /// <summary>
        /// Updates given Mass Update.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        /// <param name="massUpdate">The Mass Update details.</param>
        [HttpPut("massUpdates/{massUpdateId}")]
        public async Task UpdateMassUpdateAsync(string massUpdateId, CalendarEventMassUpdate massUpdate)
        {
            Util.ValidateArgumentNotNullOrEmpty(massUpdateId, nameof(massUpdateId));
            Util.ValidateArgumentNotNull(massUpdate, nameof(massUpdate));

            massUpdate.MassUpdateId = massUpdateId;
            await _calendarEventService.UpdateMassUpdateAsync(massUpdate);
        }

        /// <summary>
        /// Deletes Mass Update with the given Id.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        [HttpDelete("massUpdates/{massUpdateId}")]
        public async Task DeleteMassUpdateAsync(string massUpdateId)
        {
            Util.ValidateArgumentNotNullOrEmpty(massUpdateId, nameof(massUpdateId));

            await _calendarEventService.DeleteMassUpdateAsync(massUpdateId);
        }
    }
}
