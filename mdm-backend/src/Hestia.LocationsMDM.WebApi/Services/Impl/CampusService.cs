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
    /// The Campus service.
    /// </summary>
    public class CampusService : BaseLocationService, ICampusService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CampusService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="changeHistoryService">The change history service.</param>
        /// <param name="calendarEventService">The calendar event service.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public CampusService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IChangeHistoryService changeHistoryService, ICalendarEventService calendarEventService, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, changeHistoryService, appContextProvider, calendarEventService)
        {
        }

        #region Campus

        /// <summary>
        /// Gets the campus details by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>The campus details or null if not exists.</returns>
        public async Task<CampusDetailsModel> GetCampusAsync(string campusId)
        {
            string[] propsToRetrieve =
                {
                    "c.locationName as campusName",
                    "c.address",
                    "c.locationPhoneNumber as campusPhoneNumbers",
                    "c.latitude",
                    "c.longitude",
                    "c.locationID",
                    "c.pricingRegion",
                    "{" +
                    "  openForBusiness: LOWER(c.openforBusinessFlag)='y'," +
                    "  openForDisclosure: LOWER(c.openforPublicDiscloure)='y'" +
                    "} as businessInfo",
                    "c.timeZoneIdentifier",
                    "c.associate",
                    "c.calendarEventGuid"
                };

            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node='{campusId}' {ActiveRecordFilter}";
            var jObj = await LoadCampusAsync(campusId, sql);

            var model = jObj.ToObject<CampusDetailsModel>();
            model.CampusId = campusId;

            // fetch Contacts
            var associates = jObj.GetAssociates();
            model.CampusContacts = associates.ToTitledContacts();

            // get Children Summary
            model.ChildSummary = await RetrieveBaseStructureAsync(campusId);

            // load calendar events
            model.CalendarEvents = await LoadEventsAsync(NodeType.Campus, campusId, jObj);

            // load unique names of Planned events, which are associated with children
            var regionNodes = model.ChildSummary.Select(x => x.Id).ToList();
            var childLocNodes = model.ChildSummary.SelectMany(x => x.Children).Select(x => x.Id).ToList();
            var allChildrenNodes = regionNodes.Concat(childLocNodes).ToList();
            model.ChildrenPlannedEventNames = await LoadPlannedEventUniqueNamesAsync(allChildrenNodes);

            // get locations
            var locationIds = jObj.GetStringList("locationID") ?? new List<string>();

            model.LocationInfo = await GetEdmcsLocationsInfoByAddress(locationIds, model.Address);
            return model;
        }

        /// <summary>
        /// Updates the campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The updated Campus data.</param>
        public async Task UpdateCampusAsync(string campusId, CampusPatchModel model)
        {
            // remove duplicates
            model.PhoneNumbers = model.PhoneNumbers?.Distinct()?.ToList();

            // load Campus
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node='{campusId}' {ActiveRecordFilter}";
            var jObj = await LoadCampusAsync(campusId, sql);

            // set default values
            jObj["openforBusinessFlag"] = jObj.Value<string>("openforBusinessFlag") ?? "N";
            jObj["openforPublicDiscloure"] = jObj.Value<string>("openforPublicDiscloure") ?? "N";

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            // set updated values
            var newOpenForBusinessFlag = model.OpenForBusinessFlag.ToShortYesNo(emptyIfNull: false);
            var newOpenForDisclosure = model.OpenForDisclosureFlag.ToShortYesNo(emptyIfNull: false);
            newObj.UpdateOptionalProperty("openforBusinessFlag", newOpenForBusinessFlag);
            newObj.UpdateOptionalProperty("openforPublicDiscloure", newOpenForDisclosure);
            newObj.UpdateOptionalProperty("locationName", model.LocationName);
            newObj.UpdateOptionalProperty("locationPhoneNumber", model.PhoneNumbers);
            newObj.UpdateOptionalProperty("timeZoneIdentifier", model.TimeZoneIdentifier);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.CampusPartitionKey, jObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddCampusChangesAsync(jObj, newObj);

            // update Campus children's inheritable properties
            string existingTimeZone = jObj.Value<string>("timeZoneIdentifier");
            string existingOpenforBusinessFlag = jObj.Value<string>("openforBusinessFlag");
            string existingOpenforPublicDiscloure = jObj.Value<string>("openforPublicDiscloure");

            var props = new List<KeyValuePair<string, string>>();
            if (model.TimeZoneIdentifier != existingTimeZone)
            {
                props.Add(KeyValuePair.Create("timeZoneIdentifier", model.TimeZoneIdentifier));
            }
            if (newOpenForBusinessFlag != existingOpenforBusinessFlag)
            {
                props.Add(KeyValuePair.Create("openforBusinessFlag", newOpenForBusinessFlag));
            }
            if (newOpenForDisclosure != existingOpenforPublicDiscloure)
            {
                props.Add(KeyValuePair.Create("openforPublicDiscloure", newOpenForDisclosure));
            }

            if (props.Count > 0)
            {
                await UpdateCampusChildrenAsync(campusId, props);
            }
        }

        /// <summary>
        /// Updates the campus children inheritable properties.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="newProps">The new values.</param>
        /// <returns></returns>
        private async Task UpdateCampusChildrenAsync(string campusId, IList<KeyValuePair<string, string>> newProps)
        {
            var itemsToUpdate = new List<JObject>();

            TransactionalBatch regionBatch = null;
            TransactionalBatch childLocBatch = null;

            // load Campus Regions
            var regions = await LoadRegionsAsync(campusId);
            itemsToUpdate.AddRange(regions);
            if (regions.Count > 0)
            {
                // create batch for Regions
                regionBatch = CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.RegionPartitionKey);
            }

            var regionIds = regions.Select(x => x["node"].ToString()).ToList();

            // load Child Locations
            var childLocations = await LoadChildLocationsAsync(campusId, regionIds);
            itemsToUpdate.AddRange(childLocations);
            if (childLocations.Count > 0)
            {
                // create batch for child locations
                childLocBatch = CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey);
            }

            var changedRegions = new List<ChangedObject>();
            var changedChildLocs = new List<ChangedObject>();
            foreach (var existingObj in itemsToUpdate)
            {
                // perform clone using audit trail
                JObject newObj = PrepareAndCloneObject(existingObj);

                // update new values
                foreach (var prop in newProps)
                {
                    newObj.UpdateOptionalProperty(prop.Key, prop.Value);
                }

                string partitionKey = newObj["partition_key"].ToString();

                TransactionalBatch batch;
                IList<ChangedObject> changedObjects;
                if (partitionKey == CosmosConfig.RegionPartitionKey)
                {
                    batch = regionBatch;
                    changedObjects = changedRegions;
                }
                else
                {
                    batch = childLocBatch;
                    changedObjects = changedChildLocs;
                }

                // process Update Existing and Create New items to batch list
                batch
                    .ReplaceItemWithConcurrency(existingObj)
                    .CreateItem(newObj);

                changedObjects.Add(new ChangedObject
                {
                    OldObject = existingObj,
                    NewObject = newObj
                });
            }

            // update all Regions in single transaction
            await ExecuteBatchAsync(regionBatch);

            // update all child locations in single transaction
            await ExecuteBatchAsync(childLocBatch);

            // if above operations succeeded, create change history/summary
            // for Regions:
            foreach (var item in changedRegions)
            {
                await _changeHistoryService.AddRegionChangesAsync(item.OldObject, item.NewObject);
            }

            // for Child Locations:
            foreach (var item in changedChildLocs)
            {
                await _changeHistoryService.AddChildLocationChangesAsync(item.OldObject, item.NewObject);
            }
        }

        #endregion


        #region Campus Roles

        /// <summary>
        /// Gets the roles of the Campus with the given Id.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <returns>
        /// The roles of the Campus.
        /// </returns>
        public async Task<IList<CampusRoleModel>> GetCampusRolesAsync(string campusId)
        {
            await CheckCampusExistsAsync(campusId);

            string sql = $"select c.id, c.roleName from c where c.partition_key='{CosmosConfig.CampusRolePartitionKey}' and c.campusNodeId='{campusId}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<CampusRoleModel>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Creates the campus role.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="roleName">Name of the role.</param>
        public async Task CreateCampusRoleAsync(string campusId, string roleName)
        {
            string sql = $"select value Count(1) from c where c.partition_key='{CosmosConfig.CampusRolePartitionKey}'" +
                $" and c.campusNodeId='{campusId}' and LOWER(c.roleName)='{roleName.ToLower()}' {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, sql);
            if (count > 0)
            {
                throw new DataConflictException($"Campus Role with name '{roleName}' already exists.");
            }

            var roleItem = new
            {
                campusNodeId = campusId,
                roleName = roleName
            };

            await CreateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.CampusRolePartitionKey, roleItem);
        }

        #endregion


        #region Retrieve/Assign/Unassign Associates

        /// <summary>
        /// Assigns or Unassigns Associates with the given Campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        public async Task AssignCampusAssociatesAsync(string campusId, AssignAssociatesModel model, AssignMode assignMode)
        {
            var itemsToUpdate = new List<JObject>();

            // load Campus
            string sql = $"select * from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node='{campusId}' {ActiveRecordFilter}";
            var jObj = await LoadCampusAsync(campusId, sql);
            itemsToUpdate.Add(jObj);

            if (model.ApplyToChildren)
            {
                // load Campus child locations
                var childLocations = await LoadChildLocationsAsync(campusId);
                itemsToUpdate.AddRange(childLocations);
            }

            // create batch for child locations
            var childLocationBatch = itemsToUpdate.Count > 1
                ? CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey)
                : null;

            bool isCampus = true;
            foreach (var existingObj in itemsToUpdate)
            {
                // perform clone using audit trail
                JObject newObj = PrepareAndCloneObject(existingObj);

                // get existing associates
                var oldAssociates = newObj.GetAssociates();

                // construct new associates
                var newAssociates = oldAssociates.ToList();
                if (assignMode == AssignMode.Assign)
                {
                    // add new associates
                    var associateIdsToAdd = model.ContactList.Where(x => !oldAssociates.Any(y => y.AssociateId == x)).ToList();
                    var associatesToAdd = await LoadAssociatesAsync(associateIdsToAdd);
                    newAssociates.AddRange(associatesToAdd);
                }
                else
                {
                    // remove provided associates from existing list
                    newAssociates.RemoveAll(x => model.ContactList.Contains(x.AssociateId));
                } 

                // update property
                newObj["associate"] = Util.ToJToken(newAssociates);

                // campus is the first item, and needs to be updated separately
                if (isCampus)
                {
                    // update item (using Audit Trail and Optimistic Concurrency)
                    await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.CampusPartitionKey, existingObj, newObj);
                    isCampus = false;

                    // save associates changes
                    await _changeHistoryService.AddCampusAssociatesChangesAsync(newObj, oldAssociates, newAssociates);
                }
                else
                {
                    // add Update Existing and Create New items to batch list
                    childLocationBatch
                        .ReplaceItemWithConcurrency(existingObj)
                        .CreateItem(newObj);
                }
            }

            // update all child locations in single transaction
            await ExecuteBatchAsync(childLocationBatch);
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Checks the campus exists.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        private async Task CheckCampusExistsAsync(string campusId)
        {
            string sql = $"select value Count(1) from c where c.partition_key='{CosmosConfig.CampusPartitionKey}'" +
                $" and c.node='{campusId}' {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, sql);
            if (count == 0)
            {
                throw new EntityNotFoundException($"Campus with Id '{campusId}' was not found.");
            }
        }

        /// <summary>
        /// Loads the child locations.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="regionIds">The region ids.</param>
        /// <returns>
        /// The child locations
        /// </returns>
        private async Task<IList<JObject>> LoadChildLocationsAsync(string campusId, IList<string> regionIds = null)
        {
            string sql;

            if (regionIds == null)
            {
                // load Ids of Campus Regions
                sql = $"select value c.node from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.campusNodeId='{campusId}' {ActiveRecordFilter}";
                regionIds = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            }

            var regionIdsCsv = string.Join("','", regionIds);

            // load Child Locations
            sql = $"select * from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.regionNodeId in('{regionIdsCsv}') {ActiveRecordFilter}";
            var childLocations = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return childLocations;
        }

        /// <summary>
        /// Retrieves the Base hierarchy structure.
        /// </summary>
        /// <param name="campusNodeId">The base node identifier.</param>
        /// <returns>The Base hierarchy structure</returns>
        private async Task<IList<HierarchyNode>> RetrieveBaseStructureAsync(string campusNodeId)
        {
            // load Regions
            string sql = $"select c.node as id, c.regionID, c.regionName, c.locationName " +
                $"from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and " +
                $"c.campusNodeId='{campusNodeId}' {ActiveRecordFilter}";
            var regions = (await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql)).OrderBy(x => x.LocationName.ToLower());

            var regionIds = regions.Select(x => x.Id);
            var regionIdsCsv = string.Join("','", regionIds);

            // load Child Locations
            sql = $"select c.node as id, c.regionNodeId, c.locationID as locationId, " +
                $"c.locationName, c.locationType from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' " +
                $"and c.regionNodeId in('{regionIdsCsv}') {ActiveRecordFilter}";
            var childLocations = (await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql)).OrderBy(x => x.LocationName.ToLower());

            var result = new List<HierarchyNode>();
            foreach (var region in regions)
            {
                var regionNode = new HierarchyNode
                {
                    Id = region.Id,
                    Name = region.LocationName,
                    Children = new List<HierarchyNode>()
                };
                result.Add(regionNode);

                var regionChildLocs = childLocations.Where(x => x.RegionNodeId == region.Id);
                foreach (var childLoc in regionChildLocs)
                {
                    if (regionNode.Children.Any(x => x.Id == childLoc.Id))
                    {
                        // already added
                        continue;
                    }

                    var childLocNode = new HierarchyNode
                    {
                        Id = childLoc.Id,
                        Name = childLoc.LocationName,
                        LocationType = childLoc.LocationType,
                        LocationId = childLoc.LocationId
                    };
                    regionNode.Children.Add(childLocNode);
                }
            }

            result = Util.OrderStructureByName(result);
            return result;
        }        

        #endregion
    }
}
