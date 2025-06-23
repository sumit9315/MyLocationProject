using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Extensions;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Base Location service (has common functionality for Campus, Region, and Child Location services).
    /// </summary>
    public class BaseLocationService : BaseCosmosService, ILocationService
    {
        /// <summary>
        /// The change history service.
        /// </summary>
        protected readonly IChangeHistoryService _changeHistoryService;

        /// <summary>
        /// The calendar event service.
        /// </summary>
        protected readonly ICalendarEventService _calendarEventService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampusService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="changeHistoryService">The change history service.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        /// <param name="calendarEventService">The calendar event service.</param>
        public BaseLocationService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IChangeHistoryService changeHistoryService, IAppContextProvider appContextProvider, ICalendarEventService calendarEventService = null)
            : base(cosmosClient, cosmosConfig, appContextProvider)
        {
            _changeHistoryService = changeHistoryService;
            _calendarEventService = calendarEventService;
        }

        #region Calendar Events

        /// <summary>
        /// Gets the calendar events of the given location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The node.</param>
        /// <returns>The calendar events.</returns>
        public async Task<LocationCalendarEventsModel> GetEventsAsync(NodeType nodeType, string node)
        {
            // load Locations
            string partitionKey = nodeType.GetPartitionKey(CosmosConfig);
            string sql = $"SELECT value c.calendarEventGuid FROM c where c.partition_key='{partitionKey}' and c.node='{node}' {ActiveRecordFilter}";
            var eventGuids = await GetFirstOrDefaultAsync<IList<string>>(CosmosConfig.LocationsContainerName, sql, partitionKey);
            var events = await LoadEventsAsync(eventGuids);

            // for all events - check if there are inherited events
            await SetInheritedChildrenValue(nodeType, node, events);

            var result = new LocationCalendarEventsModel
            {
                CalendarEvents = events
            };

            if (nodeType != NodeType.ChildLoc)
            {
                // get Nodes of all children
                IList<string> regionNodes;
                if (nodeType == NodeType.Campus)
                {
                    regionNodes = await GetRegionIdsAsync(node);
                }
                else
                {
                    regionNodes = new List<string> { node };
                }

                var childLocNodes = await GetChildLocationIdsAsync(regionNodes);

                // load unique names of Planned events, which are associated with children
                var allChildrenNodes = regionNodes.Concat(childLocNodes).ToList();
                result.ChildrenPlannedEventNames = await LoadPlannedEventUniqueNamesAsync(allChildrenNodes);
            }
            else
            {
                result.ChildrenPlannedEventNames = new List<string>();
            }

            return result;
        }

        /// <summary>
        /// Updates events for the given Child Location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The events details model.</param>
        /// <exception cref="ArgumentException">Planned events must have 'eventId' provided</exception>
        public async Task UpdateEventsAsync(NodeType nodeType, string node, LocationEventsModel model)
        {
            // check Planned Events
            if (model.PlannedEvents.Any(x => string.IsNullOrEmpty(x.EventId)))
            {
                throw new ArgumentException("Planned events must have 'eventId' provided");
            }

            // load Locations
            string partitionKey = nodeType.GetPartitionKey(CosmosConfig);
            string sql = $"select * from c where c.partition_key='{partitionKey}' and c.node='{node}' {ActiveRecordFilter}";
            var location = await LoadLocationAsync(nodeType, node, sql);

            var createdEvents = new List<CalendarEventModel>();
            var updatedEvents = new List<CalendarEventModel>();
            var replacedEvents = new List<CalendarEventModel>(); // these are inherited and updated events

            // list of added/deleted/changed events for change history
            var addedEvents = new List<JObject>();
            var removedEvents = new List<JObject>();
            var changedEvents = new List<ChangedObject>();

            // handle Created/Updated events
            var newEvents = model.PlannedEvents.Concat(model.UnplannedEvents).ToList();
            foreach (var item in newEvents)
            {
                // set node
                item.LocationNode = node;

                // 'Create' case
                if (item.EventGuid == null)
                {
                    // Planned must have eventId
                    if (item.EventType == CalendarEventType.Planned && string.IsNullOrEmpty(item.EventId))
                    {
                        throw new ArgumentException("'EventId' must be provided for Planned events.");
                    }

                    // Unplanned should not have eventId
                    if (item.EventType == CalendarEventType.Unplanned && !string.IsNullOrEmpty(item.EventId))
                    {
                        throw new ArgumentException("'EventId' should not be provided for new Unplanned events.");
                    }

                    // create event
                    var createdObj = await _calendarEventService.CreateAsync(item);
                    createdEvents.Add(item);
                    addedEvents.Add(createdObj);
                }
                // 'Update' case
                else
                {
                    // find existing Event by Unique Id
                    var existingEventObj = await LoadCalendarEventAsync(item.EventGuid);
                    var existingEvent = existingEventObj.ToObject<CalendarEventModel>();

                    // check if at least one property changed
                    bool hasChanged = HasEventChanged(item, existingEvent);
                    if (!hasChanged)
                    {
                        continue;
                    }

                    // inherited & was not modified previously
                    if (existingEvent.LocationNode != node)
                    {
                        // Note: It happens when inherited event is updated (so cannot be at Campus level).
                        if (nodeType == NodeType.Campus)
                        {
                            throw new InvalidOperationException("LocationNode of the Event cannot be different than current location node at the Campus level.");
                        }

                        // Implementation notes: In this case new event with updated fields is created for the child location.

                        // set ParentId and clear Id to make sure new Id is generated
                        item.ParentGuid = item.EventGuid;
                        item.EventGuid = null;

                        // Name and Description cannot be updated for inherited events
                        item.EventName = existingEvent.EventName;
                        item.EventDescription = existingEvent.EventDescription;

                        // create event
                        var createdObj = await _calendarEventService.CreateAsync(item);

                        // inherited & updated list, so add to 'replaced' list
                        replacedEvents.Add(item);
                        changedEvents.Add(new ChangedObject
                        {
                            OldObject = existingEventObj,
                            NewObject = createdObj
                        });
                    }
                    else
                    {
                        // create patch model
                        var patchModel = new CalendarEventPatchModel();

                        // Date/Time/Duration/Closure can be updated for Planned & Unplanned events
                        patchModel.DisplayDuration = item.DisplayDuration;
                        patchModel.EventStartDay = item.EventStartDay;
                        patchModel.EventEndDay = item.EventEndDay;
                        patchModel.EventStartTime = item.EventStartTime;
                        patchModel.EventEndTime = item.EventEndTime;
                        patchModel.EventDuration = item.EventDuration;
                        patchModel.Closure = item.Closure;

                        // set updatable fields for Unplanned events
                        if (existingEvent.EventType == CalendarEventType.Unplanned)
                        {
                            // Name and Description cannot be updated for inherited events
                            bool isInherited = existingEvent.ParentGuid != null;
                            if (!isInherited)
                            {
                                patchModel.EventName = item.EventName;
                                patchModel.EventDescription = item.EventDescription;
                            }

                            // set fields which can be updated for Unplanned events
                            patchModel.IsFullDay = item.IsFullDay;
                        }

                        var updatedObj = await _calendarEventService.UpdateAsync(item.EventGuid, patchModel);
                        updatedEvents.Add(item);
                        changedEvents.Add(new ChangedObject
                        {
                            OldObject = existingEventObj,
                            NewObject = updatedObj
                        });
                    }
                }
            }

            var newEventGuids = newEvents
                .Select(x => x.EventGuid)
                .ToList();

            // perform clone using audit trail
            JToken newObj = PrepareAndCloneObject(location);

            // update calendar events link
            var oldEventGuids = location.GetStringList("calendarEventGuid");
            newObj["calendarEventGuid"] = newEventGuids.Count > 0 ? JArray.FromObject(newEventGuids) : new JArray();

            // update child location
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, partitionKey, location, newObj);

            // handle deleted events

            // get list of event Guids that were removed from the location
            var removedEventGuids = oldEventGuids
                .Except(newEventGuids)
                .ToList();

            // remove those which are no longer in location's list, and are directly linked to current location
            var locEventGuids = await _calendarEventService.GetLocationEventGuidsAsync(node);
            var eventGuidsToDelete = removedEventGuids.Intersect(locEventGuids).ToList();
            foreach (var eventGuid in eventGuidsToDelete)
            {
                var deletedObj = await _calendarEventService.DeleteAsync(eventGuid);
                removedEvents.Add(deletedObj);
            }

            // add removed parent objects - needed for change history
            var removedParentEventGuids = removedEventGuids.Except(eventGuidsToDelete).ToList();
            foreach (var eventGuid in removedParentEventGuids)
            {
                // If it was replaced, ignore it
                bool isReplaced = replacedEvents.Any(x => x.ParentGuid == eventGuid);
                if (!isReplaced)
                {
                    var removedParentObj = await LoadCalendarEventAsync(eventGuid);
                    removedEvents.Add(removedParentObj);
                }
            }

            // add change history
            await _changeHistoryService.AddLocationEventsChangesAsync(nodeType, location, addedEvents, removedEvents, changedEvents);

            // Handle descendant nodes
            // inheritance applies from Campus and Region nodes
            if (nodeType != NodeType.ChildLoc)
            {
                await UpdateDescendantNodes(nodeType, node, createdEvents, updatedEvents, replacedEvents, addedEvents, removedEvents, changedEvents);
            }
        }

        private async Task UpdateDescendantNodes(
            NodeType nodeType,
            string node,
            IList<CalendarEventModel> createdEvents,
            IList<CalendarEventModel> updatedEvents,
            IList<CalendarEventModel> replacedEvents,
            IList<JObject> addedParentEvents,
            IList<JObject> removedParentEvents,
            IList<ChangedObject> changedParentEvents)
        {
            var existingDescendantLocs = new List<JObject>();

            IList<string> regionNodes;
            if (nodeType == NodeType.Campus)
            {
                var regions = await LoadRegionsAsync(node);
                existingDescendantLocs.AddRange(regions);
                regionNodes = regions.Select(x => x.Value<string>("node")).ToList();
            }
            else
            {
                // only current region
                regionNodes = new List<string> { node };
            }

            // load Child Locations and add to descendant locations list
            var childLocs = await LoadChildLocationsAsync(regionNodes);
            existingDescendantLocs.AddRange(childLocs);

            // prepare descendants for update
            var newDescendantLocs = existingDescendantLocs.Select(PrepareAndCloneObject).ToList();

            // handle Created events - add new event Guids to 'calendarEventGuid'
            var createdEventGuids = createdEvents.Select(x => x.EventGuid).ToList();
            if (createdEventGuids.Count > 0)
            {
                foreach (var loc in newDescendantLocs)
                {
                    var eventGuids = loc.GetStringList("calendarEventGuid");
                    eventGuids.AddRange(createdEventGuids);
                    loc.UpdateProperty("calendarEventGuid", eventGuids);
                }
            }


            // handle Updated and Replaced events
            var updatedAndReplacedEvents = updatedEvents.Concat(replacedEvents);

            // get list of 'node' value of each descendant
            var descendantNodes = newDescendantLocs.Select(x => x.Value<string>("node")).ToList();
            var descendantNodesCsv = descendantNodes.StringJoin("','");

            // the list of events that were modified in descendant nodes, and now changed in parent node
            var changedEvents = new List<ChangedObject>();
            foreach (var ev in updatedAndReplacedEvents)
            {
                // get all Events with the same EventId, and associated with descendant nodes (via locationNode attribute)
                string query = $"select value c.eventGuid from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}" +
                    $" and c.eventId='{ev.EventId}'" +
                    $" and c.locationNode IN('{descendantNodesCsv}')";
                IList<string> eventGuidsToDelete = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, query, CosmosConfig.CalendarEventPartitionKey);

                // delete all these events (because it will inherit updated parent event)
                foreach (var eventGuid in eventGuidsToDelete)
                {
                    var deletedObj = await _calendarEventService.DeleteAsync(eventGuid);
                    // note: the EventId is not deleted, it is replaced with new data from the parent, so in change history it should look like Update operation
                    var eventId = deletedObj.Value<string>("eventId");
                    // get corresponding new object (based on the eventId)
                    var newObj = changedParentEvents.FirstOrDefault(x => x.NewObject.Value<string>("eventId") == eventId).NewObject;
                    changedEvents.Add(new ChangedObject
                    {
                        OldObject = deletedObj,
                        NewObject = newObj
                    });
                }

                // replace old (if exists) with new eventGuid in 'calendarEventGuid'
                foreach (var loc in newDescendantLocs)
                {
                    var guids = loc.GetStringList("calendarEventGuid");
                    // exclude deleted
                    guids = guids.Except(eventGuidsToDelete).ToList();

                    // exclude Guid of the parent event. This is needed when event is replaced at parent (Region) node, so need to remove reference to replaced event.
                    // example steps:
                    // 1. Event 1 created at Campus - inherited in Region and Child
                    // 2. Event 1 updated at Region - the ParentGuid is now referencing original Campus event, and it should be excluded from Child because Child will now reference new EventGuid at Region node
                    guids.Remove(ev.ParentGuid);

                    // add new, if not in the list yet
                    if (!guids.Contains(ev.EventGuid))
                    {
                        guids.Add(ev.EventGuid);
                    }
                    loc.UpdateProperty("calendarEventGuid", guids);
                }
            }


            // handle Created 'Planned' events - if same events exist at child nodes, delete them to avoid duplicates
            var createdPlannedEventIds = createdEvents
                .Where(x => x.EventType == CalendarEventType.Planned)
                .Select(x => x.EventId)
                .ToList();

            // handle Deleted events
            var removedEvents = new List<JObject>();
            if (removedParentEvents.Count > 0 || createdPlannedEventIds.Count > 0)
            {
                var removedParentEventIds = removedParentEvents.Select(x => x.Value<string>("eventId")).ToList();
                var removedParentEventGuids = removedParentEvents.Select(x => x.Value<string>("eventGuid")).ToList();
                var removedParentEventIdsCsv = removedParentEventIds.StringJoin("','");

                var createdPlannedEventIdsCsv = createdPlannedEventIds.StringJoin("','");

                // get all Events with the same EventId, and associated with descendant nodes (via locationNode attribute)
                string query = $"select value c.eventGuid from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' {ActiveRecordFilter}" +
                    $" and (c.eventId IN('{removedParentEventIdsCsv}') OR c.eventId IN('{createdPlannedEventIdsCsv}'))" +
                    $" and c.locationNode IN('{descendantNodesCsv}')";
                IList<string> eventGuidsToRemove = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, query, CosmosConfig.CalendarEventPartitionKey);

                // delete all these events (because events with same EventId were deleted at parent node)
                foreach (var eventGuid in eventGuidsToRemove)
                {
                    var deletedObj = await _calendarEventService.DeleteAsync(eventGuid);
                    removedEvents.Add(deletedObj);
                }

                // remove deleted event Guids from 'calendarEventGuid'
                foreach (var loc in newDescendantLocs)
                {
                    var eventGuids = loc.GetStringList("calendarEventGuid");
                    eventGuids = eventGuids
                        // remove deleted parent Events
                        .Except(removedParentEventGuids)
                        // remove deleted descendant node Events
                        .Except(eventGuidsToRemove)
                        .ToList();
                    loc.UpdateProperty("calendarEventGuid", eventGuids);
                }
            }

            var allRemovedEvents = removedParentEvents.Concat(removedEvents).ToList();
            var allChangedEvents = changedParentEvents.Concat(changedEvents).ToList();

            // save updates to DB
            for (int i = 0; i < existingDescendantLocs.Count; i++)
            {
                var existingLoc = existingDescendantLocs[i];
                var newLoc = newDescendantLocs[i];
                var partitionKey = newLoc.Value<string>("partition_key");

                // update only if Event Guids changed
                var existingGuids = existingLoc["calendarEventGuid"];
                var newGuids = newLoc["calendarEventGuid"];
                if (!existingGuids.IsSequenceSame(newGuids))
                {
                    await UpdateItemAsync(CosmosConfig.LocationsContainerName, partitionKey, existingLoc, newLoc);
                }

                // create change history for all descendant nodes
                var oldEventGuids = existingLoc.GetStringList("calendarEventGuid");
                var newEventGuids = newLoc.GetStringList("calendarEventGuid");

                var locNodeType = Util.GetNodeType(CosmosConfig, partitionKey);
                var locRemovedEvents = allRemovedEvents.Where(x => oldEventGuids.Contains(x.Value<string>("eventGuid"))).ToList();
                var locChangedEvents = allChangedEvents.Where(x => oldEventGuids.Contains(x.OldObject.Value<string>("eventGuid"))).ToList();

                await _changeHistoryService.AddLocationEventsChangesAsync(locNodeType, newLoc, addedParentEvents, locRemovedEvents, locChangedEvents);
            }
        }

        /// <summary>
        /// Loads the location events.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The location node.</param>
        /// <param name="location">The location object.</param>
        /// <returns>
        /// The location events.
        /// </returns>
        protected async Task<IList<CalendarEventModel>> LoadEventsAsync(NodeType nodeType, string node, JObject location)
        {
            var eventGuids = location.GetStringList("calendarEventGuid");
            var events = await LoadEventsAsync(eventGuids);

            // for all events - check if there are inherited events
            await SetInheritedChildrenValue(nodeType, node, events);

            return events;
        }

        /// <summary>
        /// Loads the unique names of the planned event which are associated with the given locations.
        /// </summary>
        /// <param name="locationNodes">The location nodes.</param>
        /// <returns>The unique names of the planned event.</returns>
        protected async Task<IList<string>> LoadPlannedEventUniqueNamesAsync(IList<string> locationNodes)
        {
            var nodesCsv = locationNodes.StringJoin("','");

            string whereCondition = $"c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventType='Planned' and c.locationNode IN('{nodesCsv}') {ActiveRecordFilter}";
            string sql = $"SELECT value groups.eventName FROM(SELECT c.eventName FROM c where {whereCondition} GROUP BY c.eventName) AS groups order by groups.eventName";

            var result = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, sql);
            return result;
        }

        protected string GetLocationsPartitionKeyFilter(bool campus = true, bool region = true, bool childLoc = true)
        {
            var partitions = new List<string>();
            if (campus)
            {
                partitions.Add(CosmosConfig.CampusPartitionKey);
            }
            if (region)
            {
                partitions.Add(CosmosConfig.RegionPartitionKey);
            }
            if (childLoc)
            {
                partitions.Add(CosmosConfig.ChildLocationPartitionKey);
            }

            var conditions = partitions.Select(x => $"c.partition_key='{x}'");
            string query = "(" + conditions.StringJoin(" or ") + ")";
            return query;
        }

        /// <summary>
        /// Loads the calendar events.
        /// </summary>
        /// <param name="eventGuids">The calendar event ids.</param>
        /// <returns>The calendar events.</returns>
        protected async Task<IList<CalendarEventModel>> LoadEventsAsync(IList<string> eventGuids)
        {
            if (eventGuids?.Count > 0)
            {
                var eventGuidsCsv = string.Join("','", eventGuids);

                // load Events
                string sql = $"select * from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventGuid IN('{eventGuidsCsv}') {ActiveRecordFilter}";
                var events = await GetAllItemsAsync<CalendarEventModel>(CosmosConfig.SecondaryContainerName, sql);

                // split to Planned and Unplanned
                var planned = events.Where(x => x.EventType == CalendarEventType.Planned);
                var unplanned = events.Where(x => x.EventType == CalendarEventType.Unplanned);

                // TODO: temp change for unplanned bulk
                var unplannedBulk = events.Where(x => x.EventType == CalendarEventType.UnplannedBulk);
                unplannedBulk.ForEach(x => x.EventType = CalendarEventType.Unplanned);

                // sort
                var plannedSorted = planned.OrderBy(x => x.EventStartDateTime);
                var unplannedSorted = unplanned.OrderBy(x => x.EventStartDateTime).ThenBy(x => x.EventEndDateTime);

                // combine sorted
                events = unplannedSorted
                    .Concat(plannedSorted)
                    .Concat(unplannedBulk) // TODO: TEMP
                    .ToList();

                return events;
            }

            return new List<CalendarEventModel>();
        }

        /// <summary>
        /// Loads the location self event guids.
        /// </summary>
        /// <param name="locationNode">The location ndoe.</param>
        /// <returns></returns>
        protected async Task<IList<string>> LoadLocationSelfEventGuids(string locationNode)
        {
            // load Event Guids
            string sql = $"select value c.eventGuid from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.locationNode='{locationNode}' {ActiveRecordFilter}";
            var eventGuids = await GetAllItemsAsync<string>(CosmosConfig.SecondaryContainerName, sql);
            return eventGuids;
        }

        /// <summary>
        /// Checks whether Events are inherited by descendant locations.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The node.</param>
        /// <param name="events">The events.</param>
        private async Task SetInheritedChildrenValue(NodeType nodeType, string node, IList<CalendarEventModel> events)
        {
            if (nodeType == NodeType.ChildLoc)
            {
                // Child Location cannot have children
                return;
            }

            //string locationPartitionsFilter = GetLocationsPartitionKeyFilter(
            //    campus: false,
            //    region: nodeType == NodeType.Campus, // Regions inherited from Campus only
            //    childLoc: true // ChildLoc inherited from Campus and Region
            //    );

            string regionsFilter = $"c.partition_key='{CosmosConfig.RegionPartitionKey}'";
            string childLocsFilter = $"c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'";

            // for Region the search should be limited to Region Children only
            if (nodeType == NodeType.Region)
            {
                childLocsFilter += $" and c.regionNodeId='{node}'";
            }

            foreach (var ev in events)
            {
                // Region Events can only be inherited from Campus Event
                if (nodeType == NodeType.Campus)
                {
                    ev.IsInheritedInRegions = await IsEventAssociatedWithLocations(ev.EventGuid, regionsFilter);
                }

                if (nodeType == NodeType.Campus && !ev.IsInheritedInRegions)
                {
                    // no need to check child locations in case it is not inherited in Regions for Campus
                    continue;
                }

                ev.IsInheritedInChildLocs = await IsEventAssociatedWithLocations(ev.EventGuid, childLocsFilter);
                // future (?): if !isInherited, check whether event with ParentGuid=ev.EventGuid exists
            }
        }

        private async Task<bool> IsEventAssociatedWithLocations(string eventGuid, string locationsSqlFilter)
        {
            string query = $"select value Count(1) from c where {locationsSqlFilter} {ActiveRecordFilter}" +
                $" and ARRAY_CONTAINS(c.calendarEventGuid, '{eventGuid}', true)";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, query);
            bool isAssociated = count > 0;
            return isAssociated;
        }

        private static bool HasEventChanged(CalendarEventModel item, CalendarEventModel existingEvent)
        {
            // check fields that can change for both Planned and Unplanned events
            bool hasChanged = item.Closure != existingEvent.Closure ||
                item.DisplayDuration != existingEvent.DisplayDuration ||
                item.EventStartDay != existingEvent.EventStartDay ||
                item.EventEndDay != existingEvent.EventEndDay ||
                item.EventStartTime != existingEvent.EventStartTime ||
                item.EventEndTime != existingEvent.EventEndTime;

            // check fields applicable for Planned events
            if (!hasChanged && item.EventType == CalendarEventType.Planned)
            {
                hasChanged = item.EventDuration != existingEvent.EventDuration;
            }

            // check fields applicable for Unplanned events
            if (!hasChanged && item.EventType == CalendarEventType.Unplanned)
            {
                hasChanged = item.EventName != existingEvent.EventName ||
                item.EventDescription != existingEvent.EventDescription ||
                item.IsFullDay != existingEvent.IsFullDay;
            }

            return hasChanged;
        }

        private async Task<JObject> LoadCalendarEventAsync(string eventGuid)
        {
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventGuid='{eventGuid}' {ActiveRecordFilter}";
            var result = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
            if (result == null)
            {
                throw new EntityNotFoundException($"Calendar Event with eventGuid '{eventGuid}' was not found.");
            }
            return result;
        }


        #endregion

        /// <summary>
        /// Loads Campus Regions.
        /// </summary>
        /// <param name="campusNode">The campus node.</param>
        /// <returns>The Campus Regions.</returns>
        protected async Task<IList<JObject>> LoadRegionsAsync(string campusNode)
        {
            string sql = $"select * from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.campusNodeId='{campusNode}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.RegionPartitionKey);
            return result;
        }

        protected async Task<IList<JObject>> LoadChildLocationsAsync(string regionNode)
        {
            string sql = $"select * from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.regionNodeId='{regionNode}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.ChildLocationPartitionKey);
            return result;
        }

        protected async Task<IList<JObject>> LoadChildLocationsAsync(IList<string> regionNodes)
        {
            var regionNodesCsv = string.Join("','", regionNodes);
            string sql = $"select * from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.regionNodeId IN('{regionNodesCsv}') {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.ChildLocationPartitionKey);
            return result;
        }

        protected async Task<IList<JObject>> LoadRegionsAsync(IList<string> nodes)
        {
            var nodesCsv = string.Join("','", nodes);
            string sql = $"select * from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node IN('{nodesCsv}') {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.RegionPartitionKey);
            return result;
        }

        protected async Task<IList<JObject>> LoadCampusesAsync(IList<string> nodes)
        {
            var nodesCsv = string.Join("','", nodes);
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node IN('{nodesCsv}') {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.CampusPartitionKey);
            return result;
        }

        [Obsolete]
        protected async Task<LocationShort> GetEdmcsLocationInfoByAddress(string locationId, NodeAddress address)
        {
            var locationIds = new List<string> { locationId };
            var locationsInfo = await GetEdmcsLocationsInfoByAddress(locationIds, address);
            var result = locationsInfo.FirstOrDefault();
            return result;
        }

        protected async Task<IList<LocationShort>> GetEdmcsLocationsInfoByAddress(IList<string> locationIds, NodePrimaryAddress address)
        {
            var result = new List<LocationShort>();
            if (locationIds.Count > 0)
            {
                string addressCondition = "";
                if (address?.AddressLine1 != null)
                {
                    addressCondition += $" and c.address.addressLine1='{address.AddressLine1.EscapeSingleQuotes()}'";
                }
                if (address?.CityName != null)
                {
                    addressCondition += $" and c.address.cityName='{address.CityName.EscapeSingleQuotes()}'";
                }
                if (address?.State != null)
                {
                    addressCondition += $" and c.address.state='{address.State}'";
                }
                if (address?.PostalCodePrimary != null)
                {
                    addressCondition += $" and c.address.postalCodePrimary='{address.PostalCodePrimary}'";
                }

                // load Locations' info from EDMCS data
                var locationIdsCsv = string.Join("','", locationIds);
                var edmcsSql =
                    $"select c.locationID, TRIM(c.locationName) as locationName from c " +
                    $"where c.partition_key='{CosmosConfig.EdmcsMasterPartitionKey}' and c.locationID IN('{locationIdsCsv}') {addressCondition} {ActiveRecordFilter} " +
                    $"group by c.locationID, TRIM(c.locationName)";

                var edmcsLocations = await GetAllItemsAsync<LocationShort>(CosmosConfig.LocationsContainerName, edmcsSql);
                var orderedLocations = edmcsLocations
                    .OrderBy(o => o.LocationId)
                    .ThenBy(x => x.LocationName)
                    .ToList();

                string currentId = null;
                foreach (var item in orderedLocations)
                {
                    if (currentId != item.LocationId)
                    {
                        result.Add(item);
                        currentId = item.LocationId;
                    }
                    else
                    {
                        result.Add(new LocationShort
                        {
                            LocationId = string.Empty,
                            LocationName = item.LocationName
                        });
                    }
                }
            }

            return result;
        }

    }
}
