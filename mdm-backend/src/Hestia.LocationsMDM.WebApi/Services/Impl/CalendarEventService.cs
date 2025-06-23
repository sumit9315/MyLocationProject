using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using MDM.Tools.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Calendar Event service.
    /// </summary>
    public class CalendarEventService : BaseCosmosService, ICalendarEventService
    {
        private static readonly string[] CalendarEventSqlSelectProps =
            {
                "c.eventGuid",
                "c.eventId",
                "c.massUpdate",
                "c.eventName",
                "c.eventDescription",
                "c.displayDuration",
                "c.eventStartDay",
                "c.eventEndDay",
                "c.eventStartTime",
                "c.eventEndTime",
                "c.eventType",
                "c.isFullDay",
                "c.eventDuration",
                "c.closure"
            };

        private readonly IChangeHistoryService _changeHistoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventService" /> class.
        /// </summary>
        /// <param name="changeHistoryService">The Change History service.</param>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public CalendarEventService(
            IChangeHistoryService changeHistoryService,
            CosmosClient cosmosClient,
            IOptions<CosmosConfig> cosmosConfig,
            IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, appContextProvider)
        {
            _changeHistoryService = changeHistoryService;
        }

        #region CRUDS

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
        [Obsolete("This method has obsolete implementation. Update is needed in case it needs to be used.")]
        public async Task<SearchResult<CalendarEventModel>> SearchAsync(
            string sortBy,
            string eventName,
            string eventStartDay,
            string eventEndDay,
            CalendarEventType? eventType,
            int pageNum,
            int pageSize)
        {
            string[] propsToRetrieve =
                {
                    "c.eventId",
                    "c.eventName",
                    "c.eventStartDay",
                    "c.eventEndDay",
                    "c.eventStartTime",
                    "c.eventEndTime",
                    "c.frequency",
                    "c.eventType",
                    "c.isFullDay"
                };

            string propList = string.Join(", ", propsToRetrieve);

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}");
            whereQuery
                .AppendContainsCondition("eventName", eventName)
                .AppendEqualsCondition("eventStartDay", eventStartDay)
                .AppendEqualsCondition("eventEndDay", eventEndDay)
                .AppendEqualsCondition("eventType", eventType?.ToString());

            // specify Order By
            string orderBy = !string.IsNullOrWhiteSpace(sortBy)
                ? $"ORDER BY c.{sortBy}"
                : string.Empty;

            var result = new SearchResult<CalendarEventModel>();

            // construct query to get page results
            int offset = (pageNum - 1) * pageSize;
            string sql = $"select {propList} from c {whereQuery} {orderBy} offset {offset} limit {pageSize}";

            // get page items
            result.Items = await GetAllItemsAsync<CalendarEventModel>(CosmosConfig.SecondaryContainerName, sql);

            // in case order by start time, do sort by end date as secondary order by
            if (sortBy == "eventStartDateTime")
            {
                result.Items = result.Items
                    .OrderBy(x => x.EventStartDateTime)
                    .ThenBy(x => x.EventEndDateTime)
                    .ToList();
            }

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.SecondaryContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets the calendar event by Id.
        /// </summary>
        /// <param name="eventGuid">The Id of the calendar event.</param>
        /// <returns>The calendar event details.</returns>
        [Obsolete("This method has obsolete implementation. Update is needed in case it needs to be used.")]
        public async Task<CalendarEventModel> GetAsync(string eventGuid)
        {
            string propList = string.Join(", ", CalendarEventSqlSelectProps);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventGuid='{eventGuid}' {ActiveRecordFilter}";
            var jObj = await LoadCalendarEventAsync(eventGuid, sql);

            var model = jObj.ToObject<CalendarEventModel>();
            return model;
        }

        /// <summary>
        /// Gets the next events, taking first 'count' events from now.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="count">The count to take.</param>
        /// <returns>Next events.</returns>
        public async Task<IList<CalendarEventModel>> GetNextAsync(CalendarEventType eventType, int count)
        {
            string[] propsToRetrieve =
                {
                    "c.massUpdateId",
                    "c.locationNode",
                    "c.eventId",
                    "c.eventName",
                    "c.eventStartDay",
                    "c.eventEndDay",
                    "c.eventStartTime",
                    "c.eventEndTime",
                    "c.isFullDay"
                };

            string propList = string.Join(", ", propsToRetrieve);

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}");
            whereQuery
                .AppendEqualsCondition("eventType", eventType.ToString())
                .AppendGreaterThanOrEqualToCondition("eventStartDateTime", DateTime.Today);

            // construct query to get page results
            string orderBy = "ORDER BY c.recEffDt DESC";
            string sql = $"select {propList} from c {whereQuery} {orderBy} offset 0 limit {count}";

            // get events
            var result = await GetAllItemsAsync<CalendarEventModel>(CosmosConfig.SecondaryContainerName, sql);

            // load event locations (corresponding to locationNode)
            var nodes = result.Select(x => x.LocationNode).Where(x => x != null);
            var nodesCsv = string.Join("','", nodes);
            sql = $"select value {{ id: c.node, name: c.locationName }} from c where" +
                $" c.node IN('{nodesCsv}') {ActiveRecordFilter}";
            var childLocs = await GetAllItemsAsync<LookupModel>(CosmosConfig.LocationsContainerName, sql);

            // load mass updates
            var massUpdateIds = result.Select(x => x.MassUpdateId).Where(x => x != null);
            var massUpdateIdsCsv = string.Join("','", massUpdateIds);
            sql = $"select value {{ id: c.massUpdateId, name: c.title }} from c where" +
                $" c.massUpdateId IN('{massUpdateIdsCsv}')" +
                $" and c.partition_key='{CosmosConfig.CalendarEventMassUpdatePartitionKey}' {ActiveRecordFilter}";
            var massUpdates = await GetAllItemsAsync<LookupModel>(CosmosConfig.SecondaryContainerName, sql);

            // update result items with Location Name
            foreach (var item in result)
            {
                var childLoc = childLocs.FirstOrDefault(x => x.Id == item.LocationNode);
                if (childLoc != null)
                {
                    item.LocationNode = childLoc.Id;
                    item.LocationName = childLoc.Name;
                }

                var massUpdate = massUpdates.FirstOrDefault(x => x.Id == item.MassUpdateId);
                if (massUpdate != null)
                {
                    item.MassUpdateId = massUpdate.Id;
                    item.MassUpdateTitle = massUpdate.Name;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the Calendar Event.
        /// </summary>
        /// <param name="model">The Calendar Event  data.</param>
        /// <returns>Created Calendar Event model.</returns>
        public async Task<JObject> CreateAsync(CalendarEventModel model)
        {
            // assign unique Id
            model.EventGuid = Guid.NewGuid().ToString();

            // generate new EventId, if not provided
            if (string.IsNullOrEmpty(model.EventId))
            {
                string eventIdPrefix = GetEventIdPrefix(model.EventType);
                model.EventId = await GetEventNextIdAsync(eventIdPrefix);
            }

            // set date and time of the event start and event end
            model.EventStartDateTime = GetEventDateTime(model.EventStartDay, model.EventStartTime);
            model.EventEndDateTime = GetEventDateTime(model.EventEndDay, model.EventEndTime);

            // create Calendar Event
            var createdObj = await CreateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.CalendarEventPartitionKey, model);
            return createdObj;
        }

        private string GetEventIdPrefix(CalendarEventType eventType)
        {
            return eventType switch
            {
                CalendarEventType.Unplanned => "EVU",
                CalendarEventType.Planned => "EVP",
                CalendarEventType.UnplannedBulk => "EVUB",
                _ => throw new ArgumentException($"Invalid Calendar Event Type: {eventType}"),
            };
        }

        /// <summary>
        /// Updates the Calendar Event.
        /// </summary>
        /// <param name="eventGuid">The calendar event Id.</param>
        /// <param name="model">The updated Calendar Event  data.</param>
        /// <returns>Updated object.</returns>
        public async Task<JObject> UpdateAsync(string eventGuid, CalendarEventPatchModel model)
        {
            // load Event Model
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventGuid='{eventGuid}' {ActiveRecordFilter}";
            var jObj = await LoadCalendarEventAsync(eventGuid, sql);

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            // set updated values
            newObj.UpdateOptionalProperty("closure", model.Closure);
            newObj.UpdateOptionalProperty("eventName", model.EventName);
            newObj.UpdateOptionalProperty("displayDuration", model.DisplayDuration);
            newObj.UpdateOptionalProperty("eventDescription", model.EventDescription);
            newObj.UpdateOptionalProperty("eventStartDay", model.EventStartDay);
            newObj.UpdateOptionalProperty("eventEndDay", model.EventEndDay);
            newObj.UpdateOptionalProperty("eventStartTime", model.EventStartTime);
            newObj.UpdateOptionalProperty("eventEndTime", model.EventEndTime);
            newObj.UpdateOptionalProperty("eventDuration", model.EventDuration);
            newObj.UpdateOptionalProperty("isFullDay", model.IsFullDay);

            // check if object actually changed
            if (!Util.HasChanges(jObj, newObj,
                "closure",
                "eventName",
                "displayDuration",
                "eventDescription",
                "eventStartDay",
                "eventEndDay",
                "eventStartTime",
                "eventEndTime",
                "eventDuration",
                "isFullDay"
                ))
            {
                return null;
            }

            // set date and time of the event start and event end
            if (model.EventStartDay != null)
            {
                var startDateTime = GetEventDateTime(model.EventStartDay, model.EventStartTime);
                newObj.UpdateOptionalProperty("eventStartDateTime", startDateTime);
            }
            if (model.EventEndDay != null)
            {
                var endDateTime = GetEventDateTime(model.EventEndDay, model.EventEndTime);
                newObj.UpdateOptionalProperty("eventEndDateTime", endDateTime);
            }

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.CalendarEventPartitionKey, jObj, newObj);

            return newObj;
        }

        /// <summary>
        /// Deletes the Calendar Event by Id.
        /// </summary>
        /// <param name="eventGuid">The Id of the calendar event.</param>
        /// <returns>The deleted object.</returns>
        public async Task<JObject> DeleteAsync(string eventGuid)
        {
            // load Calendar Event
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventGuid='{eventGuid}' {ActiveRecordFilter}";
            var jObj = await LoadCalendarEventAsync(eventGuid, sql);

            var deletedObjects = await DeleteEventsAsync(new List<JObject> { jObj });

            return deletedObjects[0];
        }

        /// <summary>
        /// Deletes the location events.
        /// </summary>
        /// <param name="locationNode">The location node.</param>
        public async Task DeleteLocationEventsAsync(string locationNode)
        {
            // get eventGuid of Events that belong to the given location
            var locationEventGuids = await GetLocationEventGuidsAsync(locationNode);

            // delete all those events
            foreach (var eventGuid in locationEventGuids)
            {
                try
                {
                    await DeleteAsync(eventGuid);
                }
                catch (EntityNotFoundException)
                {
                    // silently ignore in case some events are already deleted or not exist
                }
            }
        }

        #endregion


        /// <summary>
        /// Gets the planned events templates.
        /// </summary>
        /// <param name="excludePastEvents">Flag indicating whether past events should be excluded.</param>
        /// <returns>
        /// The planned events.
        /// </returns>
        public async Task<IList<PlannedEventTemplateModel>> GetPlannedEventTemplatesAsync(bool excludePastEvents = false)
        {
            string[] propsToRetrieve =
                {
                    "c.eventId",
                    "c.eventName",
                    "c.eventDescription",
                    "c.displayDuration",
                    "c.eventStartDay",
                    "c.eventEndDay",
                    "c.eventStartTime",
                    "c.eventEndTime",
                    "c.closure",
                    "c.eventDuration",
                    "c.displaySequence"
                };

            string propList = string.Join(", ", propsToRetrieve);

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventType='Planned' and IS_DEFINED(c.eventGuid)=false {ActiveRecordFilter}");

            // NOTE: below code is not working with current DB data, because 'eventStartDateTime' and 'eventEndDateTime' are not present in DB (in future below code can be uncommented to improve performance)
            #region code block to filter in DB instead of locally
            //// include events from current year only
            //var nextYearStartDateString = Util.ToUtcTimeString(DateTime.Today.Year + 1);
            //var currentYearStartDateString = Util.ToUtcTimeString(DateTime.Today.Year);
            //whereQuery.Append($" and c.eventEndDateTime >= '{currentYearStartDateString}' and c.eventStartDateTime < '{nextYearStartDateString}'");
            //// exclude past events if needed
            //if (!includePastEvents)
            //{
            //    var todayString = DateTime.Today.ToUtcTimeString();
            //    whereQuery.Append($" and c.eventEndDateTime >= '{todayString}'");
            //}
            #endregion

            string sql = $"select {propList} from c {whereQuery}";

            var eventTemplates = await GetAllItemsAsync<PlannedEventTemplateModel>(CosmosConfig.SecondaryContainerName, sql);

            var filteredTemplates = eventTemplates
                // take this year only
                .Where(x => DateTime.Parse(x.EventStartDay).Year == DateTime.Today.Year);

            // exclude past events if needed
            if (excludePastEvents)
            {
                filteredTemplates = filteredTemplates.Where(x => GetEventDateTime(x.EventEndDay, x.EventEndTime) >= DateTime.Now);
            }

            var result = filteredTemplates
                // sort by Sequence
                .OrderBy(x => x.DisplaySequence)
                .ToList();

            return result;
        }


        /// <summary>
        /// Gets the location event Guids.
        /// </summary>
        /// <param name="locationNode">The location node.</param>
        /// <returns>The location event Guids.</returns>
        public async Task<IList<string>> GetLocationEventGuidsAsync(string locationNode)
        {
            // load Calendar Event
            string sql = $"select value c.eventGuid from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.locationNode='{locationNode}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.CalendarEventPartitionKey);
            return result;
        }

        /// <summary>
        /// Gets the event Ids by given event Guids.
        /// </summary>
        /// <param name="eventGuids">The event guids.</param>
        /// <returns>The event Ids.</returns>
        public async Task<IList<string>> GetEventIdsAsync(IList<string> eventGuids)
        {
            // return empty when parameter is empty
            if (eventGuids.Count == 0)
            {
                return new List<string> { };
            }

            // load Calendar Event
            var querySb = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}");
            querySb.AppendContainsCondition("eventGuid", eventGuids);

            string sql = $"select value c.eventId from c {querySb}";
            var result = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.CalendarEventPartitionKey);
            return result;
        }

        #region Mass Update

        /// <summary>
        /// Gets all Calendar Event Mass Updates.
        /// </summary>
        /// <returns>All Calendar Event Mass Updates.</returns>
        public async Task<SearchResult<CalendarEventMassUpdate>> SearchMassUpdatesAsync(SearchCriteria criteria)
        {
            string whereExpr = $"where c.partition_key='{CosmosConfig.CalendarEventMassUpdatePartitionKey}' {ActiveRecordFilter}";

            // construct query to get page results
            string orderBy = $"ORDER BY c.{criteria.SortBy} {criteria.SortOrder}";
            string paging = GetPagingStatement(criteria.PageNum, criteria.PageSize);

            // get page items
            string sql = $"SELECT * FROM c {whereExpr} {orderBy} {paging}";
            var items = await GetAllItemsAsync<CalendarEventMassUpdate>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.CalendarEventMassUpdatePartitionKey);

            // set Filter Description for all items
            items.ForEach(x => x.FilterDescription = Util.GetCalendarEventMassUpdateFilterDescription(x.Filter));

            // set calendar events count for all
            await SetMassUpdateEvents(items);

            var result = new SearchResult<CalendarEventMassUpdate>(items);

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereExpr}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.SecondaryContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets Mass Update details.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        /// <returns>The Mass Update details.</returns>
        public async Task<CalendarEventMassUpdate> GetMassUpdateAsync(string massUpdateId)
        {
            // get details
            var massUpdate = await GetMassUpdateItemAsync(massUpdateId);

            // get events
            massUpdate.CalendarEvents = await GetMassUpdateEventsAsync(massUpdateId);

            // calculate excluded list
            var massEventGuids = massUpdate.CalendarEvents.Select(x => x.EventGuid).ToList();
            massUpdate.Filter.ExcludedLocationNodes = await CalculateExcludedLocationNodes(massUpdate.Filter, massEventGuids);

            return massUpdate;
        }

        /// <summary>
        /// Creates new Calendar Event Mass Update.
        /// </summary>
        /// <param name="massUpdate">Mass update details.</param>
        /// <returns>Created Mass Update.</returns>
        public async Task<CalendarEventMassUpdate> CreateMassUpdateAsync(CalendarEventMassUpdate massUpdate)
        {
            ValidateMassUpdateModel(massUpdate);

            // find all Child Locations matching new filter
            IList<JObject> childLocs = await SearchChildLocationsAsync(massUpdate.Filter);
            if (childLocs.Count == 0)
            {
                throw new ArgumentException("No Child Locations found matching given filter criteria.");
            }

            // calculate Mass Update Id
            massUpdate.MassUpdateId = await GetMassUpdateNextIdAsync();
            massUpdate.CreatedDate = DateTime.UtcNow;
            // massUpdate.LastUpdatedDate = DateTime.Now;

            // create events
            foreach (var calendarEvent in massUpdate.CalendarEvents)
            {
                calendarEvent.MassUpdateId = massUpdate.MassUpdateId;
                await CreateAsync(calendarEvent);
            }

            // create Mass Update object
            var massUpdateObj = await CreateItemAsync(
                CosmosConfig.SecondaryContainerName,
                CosmosConfig.CalendarEventMassUpdatePartitionKey,
                massUpdate);

            // create batch to update Child Locations
            var batch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey));

            // get all Planned Events for affected locations
            var allExistingPlannedEvents = await GetPlannedEventsForLocations(childLocs);

            // assign events to all locations
            foreach (var item in childLocs)
            {
                // perform clone using audit trail
                JObject updatedItem = PrepareAndCloneObject(item);
                var eventGuids = updatedItem.GetStringList("calendarEventGuid");

                // remove Planned Events that got overwritten by this Mass Update
                await RemoveOverwrittenEvents(item.Node(), eventGuids, allExistingPlannedEvents, massUpdate.CalendarEvents);

                // add new Event Guids to the list
                foreach (var calendarEvent in massUpdate.CalendarEvents)
                {
                    eventGuids.Add(calendarEvent.EventGuid);
                }

                updatedItem.UpdateProperty("calendarEventGuid", eventGuids);

                // add Update Existing and Create New items to batch list
                batch.UpdateItem(item, updatedItem);
            }

            // execute batch update for all locations
            await batch.ExecuteAsync();

            // create history
            await _changeHistoryService.AddCalendarEventMassUpdateCreatedChangesAsync(massUpdate, childLocs);

            return massUpdate;
        }

        /// <summary>
        /// Updates given Mass Update.
        /// </summary>
        /// <param name="massUpdate">The Mass Update details.</param>
        public async Task UpdateMassUpdateAsync(CalendarEventMassUpdate massUpdate)
        {
            ValidateMassUpdateModel(massUpdate);
            JObject existingItem = await GetMassUpdateDbObjectAsync(massUpdate.MassUpdateId);
            IList<JObject> oldEvents = await GetMassUpdateEventDbObjectsAsync(massUpdate.MassUpdateId);

            // get all Child Locations that have previous events assigned
            var filter = new CalendarEventMassUpdateFilter
            {
                CalendarEventGuids = oldEvents.Select(x => x.Value<string>("eventGuid")).ToList()
            };
            IList<JObject> oldChildLocs = await SearchChildLocationsAsync(filter);

            // find all Child Locations matching new filter
            IList<JObject> newChildLocs = await SearchChildLocationsAsync(massUpdate.Filter);
            if (newChildLocs.Count == 0)
            {
                throw new ArgumentException("No Child Locations found matching given filter criteria.");
            }

            // Delete events
            var eventsToDelete = oldEvents
                .Where(x => !massUpdate.CalendarEvents.Any(y => y.EventGuid == x.Value<string>("eventGuid")))
                .ToList();
            await DeleteEventsAsync(eventsToDelete);

            // Update/Create events
            await UpsertMassUpdateCalendarEvents(massUpdate.MassUpdateId, massUpdate.CalendarEvents);

            // get list of Child Locations which were excluded from the Mass Update
            var excludedChildLocs = oldChildLocs
                .Where(x => !newChildLocs.Any(y => y.Node() == x.Node()))
                .ToList();

            // update all Event Guid list for all affected Child Locations
            await UpdateMassUpdateChildLocationsAsync(excludedChildLocs, newChildLocs, oldEvents, massUpdate.CalendarEvents);

            // perform clone using audit trail
            JObject updatedItem = PrepareAndCloneObject(existingItem);

            // update properties
            updatedItem.UpdateProperty("title", massUpdate.Title);
            updatedItem.UpdateProperty("description", massUpdate.Description);

            // only update Mass Update if it's properties were changed
            if (Util.HasChanges(existingItem, updatedItem,
                "title",
                "description"))
            {
                // update item (using Audit Trail and Optimistic Concurrency)
                await UpdateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.CalendarEventMassUpdatePartitionKey, existingItem, updatedItem);

                // create history
                var oldMassUpdateModel = existingItem.ToObject<CalendarEventMassUpdate>();
                await _changeHistoryService.AddCalendarEventMassUpdateUpdatedChangesAsync(oldMassUpdateModel, massUpdate);
            }
        }

        /// <summary>
        /// Deletes Mass Update with the given Id.
        /// </summary>
        /// <param name="massUpdateId">The Mass Update Id.</param>
        public async Task DeleteMassUpdateAsync(string massUpdateId)
        {
            JObject existingItem = await GetMassUpdateDbObjectAsync(massUpdateId);
            IList<JObject> massEvents = await GetMassUpdateEventDbObjectsAsync(massUpdateId);

            if (massEvents.Count > 0)
            {
                // get all Child Locations that have mass events assigned
                var filter = new CalendarEventMassUpdateFilter
                {
                    CalendarEventGuids = massEvents.Select(x => x.Value<string>("eventGuid")).ToList()
                };
                IList<JObject> includedChildLocs = await SearchChildLocationsAsync(filter);

                // Delete Mass events
                await DeleteEventsAsync(massEvents);

                // update all Event Guid list for all affected Child Locations
                await UpdateMassUpdateChildLocationsAsync(includedChildLocs, null, massEvents, null);
            }

            // perform clone using audit trail
            JObject updatedItem = PrepareDeleteAndCloneObject(existingItem);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.CalendarEventMassUpdatePartitionKey, existingItem, updatedItem);

            // create history
            var deletedMassUpdate = existingItem.ToObject<CalendarEventMassUpdate>();
            await _changeHistoryService.AddCalendarEventMassUpdateDeletedChangesAsync(deletedMassUpdate, null);
        }

        #endregion


        #region Mass Update helper methods

        private async Task SetMassUpdateEvents(IList<CalendarEventMassUpdate> items)
        {
            // skip data load in case there are no items
            if (items.Count == 0)
            {
                return;
            }

            var massUpdateIds = items.Select(x => x.MassUpdateId).ToList();

            // construct WHERE expression
            var whereExpr = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}");
            whereExpr.AppendContainsCondition("massUpdateId", massUpdateIds);

            string sql = $"SELECT c.massUpdateId, c.eventGuid, c.eventId FROM c {whereExpr}";
            var events = await GetAllItemsAsync<CalendarEventModel>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.CalendarEventPartitionKey);

            foreach (var item in items)
            {
                item.CalendarEvents = events.Where(x => x.MassUpdateId == item.MassUpdateId).ToList();
            }
        }

        private async Task<IList<string>> CalculateExcludedLocationNodes(CalendarEventMassUpdateFilter massUpdateFilter, IList<string> massEventGuids)
        {
            // search location Ids based on filter (without excluded items)
            var filteredLocations = await SearchChildLocationsAsync(massUpdateFilter, "c.node");
            var filteredLocationNodes = filteredLocations.Select(x => x.Value<string>("node")).ToList();

            // search locations which actually have at least one of those events
            var locationsWithEventsFilter = new CalendarEventMassUpdateFilter
            {
                CalendarEventGuids = massEventGuids
            };
            var actualLocations = await SearchChildLocationsAsync(locationsWithEventsFilter, "c.node");
            var actualLocationNodes = actualLocations.Select(x => x.Value<string>("node")).ToList();

            // calculate excluded list
            var result = filteredLocationNodes.Except(actualLocationNodes).ToList();
            return result;
        }

        private async Task RemoveOverwrittenEvents(string childLocNode, IList<string> eventGuids, IList<CalendarEventModel> existingPlannedEvents, IList<CalendarEventModel> newEvents)
        {
            // remove overwritten (Planned) events
            foreach (var eventGuid in eventGuids.ToList())
            {
                var itemEvent = existingPlannedEvents.FirstOrDefault(x => x.EventGuid == eventGuid);
                if (itemEvent == null)
                {
                    continue;
                }

                // check if any of the new Events have the same Event ID
                if (newEvents.Any(x => x.EventId == itemEvent.EventId))
                {
                    eventGuids.Remove(eventGuid);

                    // delete calendar event in case it belongs to this location
                    if (itemEvent.LocationNode == childLocNode)
                    {
                        await DeleteAsync(eventGuid);
                    }
                }
            }

        }

        private async Task<IList<CalendarEventModel>> GetPlannedEventsForLocations(IList<JObject> locationObjs)
        {
            var eventGuids = locationObjs
                .SelectMany(x => x.GetStringList("calendarEventGuid"))
                .Distinct()
                .ToList();

            // construct WHERE expression
            var whereExpr = new StringBuilder($"where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}");
            whereExpr.AppendEqualsCondition("eventType", "Planned");
            whereExpr.AppendContainsCondition("eventGuid", eventGuids);

            string sql = $"SELECT c.eventGuid, c.eventId, c.locationNode FROM c {whereExpr}";
            var events = await GetAllItemsAsync<CalendarEventModel>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.CalendarEventPartitionKey);

            return events;
        }

        private async Task UpdateMassUpdateChildLocationsAsync(
            IList<JObject> excludedChildLocs,
            IList<JObject> includedChildLocs,
            IList<JObject> oldEvents,
            IList<CalendarEventModel> newEvents)
        {
            // construct Old/New event guid lists
            var oldMassEventGuids = oldEvents.Select(x => x.Value<string>("eventGuid")).ToList();

            // create batch to update Child Locations
            var batch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey));
            bool hasChanges = false;

            // remove existing Event Guids from excluded Child Locs list
            foreach (var item in excludedChildLocs)
            {
                // perform clone using audit trail
                JObject updatedItem = PrepareAndCloneObject(item);

                // get existing Event Guids list 
                var eventGuids = updatedItem.GetStringList("calendarEventGuid");

                // remove old event guids
                var updatedEventGuids = eventGuids.Except(oldMassEventGuids).ToList();

                // check that list actually changed
                if (updatedEventGuids.Count == eventGuids.Count)
                {
                    continue;
                }

                // update attribute
                updatedItem.UpdateProperty("calendarEventGuid", updatedEventGuids);
                hasChanges = true;

                // add Update Existing and Create New items to batch list
                batch.UpdateItem(item, updatedItem);

                // add history
                var removedEventGuids = eventGuids.Except(updatedEventGuids);
                var removedEventIds = oldEvents
                    .Where(x => removedEventGuids.Contains(x.Value<string>("eventGuid")))
                    .Select(x => x.Value<string>("eventId"))
                    .ToList();
                await _changeHistoryService.AddChildLocationCalendarEventMassUpdateChangesAsync(item, removedEventIds, null);
            }

            // 'includedChildLocs' is null when Events should only be removed from excluded locations list,
            // which happens during DELETE operation
            if (includedChildLocs != null)
            {
                var newMassEventGuids = newEvents.Select(x => x.EventGuid).ToList();

                // get all Planned Events for included locations
                var allExistingPlannedEvents = await GetPlannedEventsForLocations(includedChildLocs);

                // set Event Guids to new Child Locs list
                foreach (var item in includedChildLocs)
                {
                    // perform clone using audit trail
                    JObject updatedItem = PrepareAndCloneObject(item);

                    // get existing Event Guids list 
                    var oldEventGuids = updatedItem.GetStringList("calendarEventGuid");
                    var newEventGuids = oldEventGuids.ToList();

                    // remove Planned Events that got overwritten by this Mass Update
                    await RemoveOverwrittenEvents(item.Node(), newEventGuids, allExistingPlannedEvents, newEvents);

                    // remove old event guids and add new event guids
                    newEventGuids = newEventGuids.Except(oldMassEventGuids).Union(newMassEventGuids).ToList();

                    // check that list actually changed
                    var removedEventGuids = oldEventGuids.Except(newEventGuids);
                    var addedEventGuids = newEventGuids.Except(oldEventGuids);
                    if (!removedEventGuids.Any() && !addedEventGuids.Any())
                    {
                        continue;
                    }

                    // update attribute
                    updatedItem.UpdateProperty("calendarEventGuid", newEventGuids);

                    // add Update Existing and Create New items to batch list
                    batch.UpdateItem(item, updatedItem);
                    hasChanges = true;

                    // add history
                    var removedEventIds = oldEvents
                        .Where(x => removedEventGuids.Contains(x.Value<string>("eventGuid")))
                        .Select(x => x.Value<string>("eventId"))
                        .ToList();
                    var addedEventIds = newEvents
                        .Where(x => addedEventGuids.Contains(x.EventGuid))
                        .Select(x => x.EventId)
                        .ToList();
                    await _changeHistoryService.AddChildLocationCalendarEventMassUpdateChangesAsync(item, removedEventIds, addedEventIds);
                }
            }

            // execute batch update for all locations
            if (hasChanges)
            {
                await batch.ExecuteAsync();
            }
        }

        private async Task UpsertMassUpdateCalendarEvents(string massUpdateId, IList<CalendarEventModel> calendarEvents)
        {
            foreach (var calendarEvent in calendarEvents)
            {
                if (calendarEvent.EventGuid == null)
                {
                    calendarEvent.MassUpdateId = massUpdateId;
                    await CreateAsync(calendarEvent);
                }
                else
                {
                    await UpdateAsync(calendarEvent.EventGuid, new CalendarEventPatchModel
                    {
                        EventName = calendarEvent.EventName,
                        EventDescription = calendarEvent.EventDescription,
                        DisplayDuration = calendarEvent.DisplayDuration,
                        EventStartDay = calendarEvent.EventStartDay,
                        EventStartTime = calendarEvent.EventStartTime,
                        EventEndDay = calendarEvent.EventEndDay,
                        EventEndTime = calendarEvent.EventEndTime,
                        Closure = calendarEvent.Closure,
                        EventDuration = calendarEvent.EventDuration,
                        IsFullDay = calendarEvent.IsFullDay
                    });
                }
            }
        }

        private void ValidateMassUpdateModel(CalendarEventMassUpdate massUpdate)
        {
            // validate data
            Util.ValidateArgumentNotNull(massUpdate, nameof(massUpdate));
            Util.ValidateArgumentNotNullOrEmpty(massUpdate.Title, nameof(massUpdate.Title));
            Util.ValidateArgumentNotNull(massUpdate.Filter, nameof(massUpdate.Filter));
            Util.ValidateArgumentNotNullOrEmpty(massUpdate.CalendarEvents, nameof(massUpdate.CalendarEvents));
        }

        /// <summary>
        /// Searches the child locations matching given criteria.
        /// </summary>
        /// <param name="filter">The mass update filter.</param>
        /// <param name="propsToRetrieve">The csv of properties to retrieve.</param>
        /// <returns>
        /// The matched child locations.
        /// </returns>
        private async Task<IList<JObject>> SearchChildLocationsAsync(CalendarEventMassUpdateFilter filter, string propsToRetrieve = "*")
        {
            // filter by partition (Campus/Region/ChildLoc)
            string partitionKeyFilter = $"c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'";

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where {partitionKeyFilter} {ActiveRecordFilter}");

            // parse multivalue criteria
            string[] locationNodes = Util.SplitMultivalueCriteria(filter.ChildLocNodes);

            whereQuery
                .AppendContainsCondition("address.cityName", filter.CityName)
                .AppendEqualsCondition("address.state", filter.State)
                .AppendEqualsCondition("address.countryName", filter.CountryName)
                .AppendEqualsCondition("kob", filter.Kob)
                .AppendEqualsCondition("locationType", filter.LocationType)
                .AppendContainsCondition("node", locationNodes)
                .AppendNotContainsCondition("node", filter.ExcludedLocationNodes);

            // CostCenterId filter should use sub-query to check 'financialData' array
            string[] costCenterIds = Util.SplitMultivalueCriteria(filter.CostCenterIds);
            if (costCenterIds?.Length > 0)
            {
                string valuesCsv = costCenterIds.Select(x => x.ToUpperInvariant()).StringJoin("','");
                string costCenterIdFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE fd.costCenterId IN('{valuesCsv}'))";
                whereQuery.Append($" AND {costCenterIdFilter}");
            }

            // CalendarEventGuids filter should use sub-query to check 'calendarEventGuid' array
            if (filter.CalendarEventGuids?.Count > 0)
            {
                string valuesCsv = filter.CalendarEventGuids.StringJoin("','");
                string eventGuidFilter = $"EXISTS(SELECT VALUE ce FROM ce IN c.calendarEventGuid WHERE ce IN('{valuesCsv}'))";
                whereQuery.Append($" AND {eventGuidFilter}");
            }

            // construct SQL query and get all items
            string sql = $"select {propsToRetrieve} from c {whereQuery}";
            var items = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return items;
        }

        /// <summary>
        /// Gets the Mass Update next Id.
        /// </summary>
        /// <returns>Next Id.</returns>
        private async Task<string> GetMassUpdateNextIdAsync()
        {
            const string prefix = "CEMU";
            string sql = $"SELECT VALUE MAX(c.massUpdateId) FROM c WHERE c.partition_key='{CosmosConfig.CalendarEventMassUpdatePartitionKey}' and STARTSWITH(c.massUpdateId, '{prefix}')";

            string maxId = await GetValueAsync<string>(CosmosConfig.SecondaryContainerName, sql);

            int idNumber = maxId == null
                ? 0
                : Convert.ToInt32(maxId.Substring(prefix.Length));

            var nextIdNumber = idNumber + 1;
            string nextId = $"{prefix}{nextIdNumber:D8}";
            return nextId;
        }

        private async Task<CalendarEventMassUpdate> GetMassUpdateItemAsync(string massUpdateId)
        {
            string[] propsToRetrieve =
                {
                    "c.massUpdateId",
                    "c.title",
                    "c.description",
                    "c.filter",
                    "c.createdBy",
                    "c.createdDate",
                    "c.lastUpdatedBy",
                    "c.recEffDt"
                };

            string propList = string.Join(", ", propsToRetrieve);
            var massUpdateObj = await GetMassUpdateDbObjectAsync(massUpdateId, propList);
            var result = massUpdateObj.ToObject<CalendarEventMassUpdate>();
            return result;
        }

        private async Task<JObject> GetMassUpdateDbObjectAsync(string massUpdateId, string propList = "*")
        {
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CalendarEventMassUpdatePartitionKey}' and c.massUpdateId='{massUpdateId}' {ActiveRecordFilter}";
            var result = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
            if (result == null)
            {
                throw new EntityNotFoundException($"Calendar Event Mass Update with ID '{massUpdateId}' was not found.");
            }
            return result;
        }

        private async Task<IList<CalendarEventModel>> GetMassUpdateEventsAsync(string massUpdateId)
        {
            string propList = string.Join(", ", CalendarEventSqlSelectProps);
            var eventObjects = await GetMassUpdateEventDbObjectsAsync(massUpdateId, propList);
            var result = eventObjects
                .Select(x => x.ToObject<CalendarEventModel>())
                .ToList();
            return result;
        }

        private async Task<IList<JObject>> GetMassUpdateEventDbObjectsAsync(string massUpdateId, string propList = "*")
        {
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.massUpdateId='{massUpdateId}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
            return result;
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Loads the Calendar Event.
        /// </summary>
        /// <param name="eventGuid">The Id of the Calendar Event.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching Calendar Event.</returns>
        private async Task<JObject> LoadCalendarEventAsync(string eventGuid, string sql)
        {
            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
            var jObj = queryResult.FirstOrDefault();
            if (jObj == null)
            {
                throw new EntityNotFoundException($"Calendar Event with Guid '{eventGuid}' was not found.");
            }

            return jObj;
        }

        /// <summary>
        /// Gets the event next Id.
        /// </summary>
        /// <returns>Next Id.</returns>
        private async Task<string> GetEventNextIdAsync(string prefix)
        {
            string sql = $"SELECT VALUE MAX(c.eventId) FROM c WHERE c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and STARTSWITH(c.eventId, '{prefix}')";

            string maxId = await GetValueAsync<string>(CosmosConfig.SecondaryContainerName, sql);

            IEnumerable<char> idInitials;
            int idNumber;

            if (maxId == null)
            {
                idNumber = 0;
            }
            else
            {
                idInitials = maxId.TakeWhile(x => char.IsLetter(x));
                idNumber = Convert.ToInt32(maxId.Substring(idInitials.Count()));
            }

            var nextIdNumber = idNumber + 1;
            string nextId = $"{prefix}{nextIdNumber:D8}";
            return nextId;
        }

        /// <summary>
        /// Gets the event date time.
        /// </summary>
        /// <param name="eventDay">The event day.</param>
        /// <param name="eventTime">The event time.</param>
        /// <returns></returns>
        private static DateTime GetEventDateTime(string eventDay, string eventTime)
        {
            var datetime = DateTime.Parse(eventDay);
            if (!string.IsNullOrWhiteSpace(eventTime))
            {
                var time = TimeSpan.Parse(eventTime);
                datetime = datetime.Add(time);
            }
            return datetime;
        }

        /// <summary>
        /// Deletes given Calendar Events.
        /// </summary>
        /// <param name="events">The calendar events to delete.</param>
        /// <returns>The deleted object.</returns>
        private async Task<IList<JObject>> DeleteEventsAsync(IList<JObject> events)
        {
            // NOTE: consider using batch in the future

            var deletedObjects = new List<JObject>(events.Count);
            foreach (var jObj in events)
            {
                // perform clone using audit trail
                JObject newObj = PrepareDeleteAndCloneObject(jObj);

                // update item (using Audit Trail and Optimistic Concurrency)
                await UpdateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.CalendarEventPartitionKey, jObj, newObj);

                deletedObjects.Add(newObj);
            }

            return deletedObjects;
        }

        #endregion
    }
}
