using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Extensions;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Region service.
    /// </summary>
    public class RegionService : BaseLocationService, IRegionService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegionService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="changeHistoryService">The change history service.</param>
        /// <param name="calendarEventService">The calendar event service.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public RegionService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IChangeHistoryService changeHistoryService, ICalendarEventService calendarEventService, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, changeHistoryService, appContextProvider, calendarEventService)
        {
        }

        #region CRUDS

        /// <summary>
        /// Gets the region details by Id.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <returns>
        /// The region details.
        /// </returns>
        public async Task<RegionDetailsModel> GetAsync(string regionId)
        {
            string[] propsToRetrieve =
                {
                    "c.regionName",
                    "c.locationName",
                    "c.regionID as regionId",
                    "c.pricingRegion",
                    "c.campusNodeId",
                    "c.address",
                    "c.oracleCompanyID",
                    "{" +
                    "  openForBusiness: LOWER(c.openforBusinessFlag)='y'," +
                    "  openForDisclosure: LOWER(c.openforPublicDiscloure)='y'" +
                    "} as businessInfo",
                    "c.locationPhoneNumber as regionPhoneNumbers",
                    "c.latitude",
                    "c.longitude",
                    "ARRAY_SLICE(c.associate, 0, 5) AS associate",
                    "c.calendarEventGuid",
                    "c.timeZoneIdentifier"
                };

            // load Region
            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionId}' {ActiveRecordFilter}";
            JObject jObj = await LoadRegionAsync(regionId, sql);

            var model = jObj.ToObject<RegionDetailsModel>();
            model.Id = regionId;

            // Fusion Ids (need to support both array and single value)
            model.FusionId = jObj.GetStringList("oracleCompanyID");

            // fetch Contacts
            var associates = jObj.GetAssociates();
            model.RegionContacts = associates.ToTitledContacts();

            // fetch Children Summary
            model.ChildSummary = await RetrieveRegionBaseStructureAsync(regionId);

            // load calendar events
            model.CalendarEvents = await LoadEventsAsync(NodeType.Region, regionId, jObj);

            // load unique names of Planned events, which are associated with children
            var childLocNodes = model.ChildSummary.Select(x => x.Id).ToList();
            model.ChildrenPlannedEventNames = await LoadPlannedEventUniqueNamesAsync(childLocNodes);

            // get locations
            model.LocationInfo = new List<LocationShort>();
            var locationIds = jObj.GetStringList("locationID");
            if (locationIds.Count > 0)
            {
                var allChildLocs = model.ChildSummary.SelectMany(x => x.Children);
                foreach (var locationId in locationIds)
                {
                    var childLoc = allChildLocs.FirstOrDefault(x => x.LocationId == locationId);
                    model.LocationInfo.Add(new LocationShort
                    {
                        LocationId = locationId,
                        LocationName = childLoc?.Name
                    });
                }
            }

            return model;
        }

        /// <summary>
        /// Updates the region.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <param name="model">The updated Region data.</param>
        public async Task UpdateAsync(string regionId, RegionPatchModel model)
        {
            // remove duplicates
            model.PhoneNumbers = model.PhoneNumbers?.Distinct()?.ToList();

            // load Region
            string sql = $"select * from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionId}' {ActiveRecordFilter}";
            var jObj = await LoadRegionAsync(regionId, sql);

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
            newObj.UpdateOptionalProperty("locationPhoneNumber", model.PhoneNumbers);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.RegionPartitionKey, jObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddRegionChangesAsync(jObj, newObj);

            // update region children's inheritable properties
            string existingOpenforBusinessFlag = jObj.Value<string>("openforBusinessFlag");
            string existingOpenforPublicDiscloure = jObj.Value<string>("openforPublicDiscloure");

            var props = new List<KeyValuePair<string, string>>();
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
                await UpdateRegionChildrenAsync(regionId, props);
            }
        }

        #endregion


        #region Retrieve/Assign/Unassign Associates

        /// <summary>
        /// Assigns or Unassigns Associates with the given Region.
        /// </summary>
        /// <param name="regionId">The Region Id.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        public async Task AssignAssociatesAsync(string regionId, AssignAssociatesModel model, AssignMode assignMode)
        {
            // load Child Location
            string sql = $"select * from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionId}' {ActiveRecordFilter}";
            var jObj = await LoadRegionAsync(regionId, sql);

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

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

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.RegionPartitionKey, jObj, newObj);

            // save associates changes
            await _changeHistoryService.AddRegionAssociatesChangesAsync(newObj, oldAssociates, newAssociates);
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Updates the campus children inheritable properties.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <param name="newProps">The new properties.</param>
        private async Task UpdateRegionChildrenAsync(string regionNodeId, IList<KeyValuePair<string, string>> newProps)
        {
            // load Child Locations
            var childLocations = await LoadChildLocationsAsync(regionNodeId);
            if (childLocations.Count == 0)
            {
                // nothing to update
                return;
            }

            // create batch for child locations
            TransactionalBatch childLocBatch = CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey);

            var changedChildLocs = new List<ChangedObject>();
            foreach (var existingObj in childLocations)
            {
                // perform clone using audit trail
                JObject newObj = PrepareAndCloneObject(existingObj);

                // update new values
                foreach (var prop in newProps)
                {
                    newObj.UpdateOptionalProperty(prop.Key, prop.Value);
                }

                // process Update Existing and Create New items to batch list
                childLocBatch
                    .ReplaceItemWithConcurrency(existingObj)
                    .CreateItem(newObj);

                changedChildLocs.Add(new ChangedObject
                {
                    OldObject = existingObj,
                    NewObject = newObj
                });
            }

            // update all child locations in single transaction
            await ExecuteBatchAsync(childLocBatch);

            // for Child Locations:
            foreach (var item in changedChildLocs)
            {
                await _changeHistoryService.AddChildLocationChangesAsync(item.OldObject, item.NewObject);
            }
        }

        /// <summary>
        /// Retrieves the Base hierarchy structure for the region.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <returns>
        /// The Region Base hierarchy structure.
        /// </returns>
        private async Task<IList<HierarchyNode>> RetrieveRegionBaseStructureAsync(string regionNodeId)
        {
            // load Child Locations
            var sql = $"select c.node as id, c.regionNodeId, c.address, c.locationID as locationId, c.locationName," +
                $" c.locationType from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and " +
                $"c.regionNodeId='{regionNodeId}' {ActiveRecordFilter}";
            var childLocations = await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql);

            var result = new List<HierarchyNode>();
            var regionChildLocs = childLocations.Where(x => x.RegionNodeId == regionNodeId).OrderBy(o => o.LocationName.ToLower());
            foreach (var childLoc in regionChildLocs)
            {
                if (result.Any(x => x.Id == childLoc.Id))
                {
                    // already added
                    continue;
                }

                var childLocNode = new HierarchyNode
                {
                    Id = childLoc.Id,
                    Name = childLoc.LocationName,
                    Address = childLoc.Address,
                    LocationType = childLoc.LocationType,
                    LocationId = childLoc.LocationId
                };
                result.Add(childLocNode);
            }

            return result.OrderBy(o => o.Name).ToList();
        }

        #endregion
    }
}
