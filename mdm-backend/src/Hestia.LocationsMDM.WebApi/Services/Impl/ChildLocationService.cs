using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Common.Constants;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Models;
using MDM.Tools.Common;
using MDM.Tools.Common.ValueCalculators;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Campus service.
    /// </summary>
    public class ChildLocationService : BaseLocationService, IChildLocationService
    {
        /// <summary>
        /// The location type prefixes to use when creating location.
        /// </summary>
        private static readonly IDictionary<string, string> LocationTypePrefixes = new Dictionary<string, string>
        {
            [LocationTypes.AuxiliaryStorage] = "AS",
            [LocationTypes.Counter] = "CO",
            [LocationTypes.DistributionCenter] = "DC",
            [LocationTypes.MarketDistributionCenter] = "MDC",
            [LocationTypes.OfficeSpace] = "OS",
            [LocationTypes.OutsideStorageYard] = "OY",
            [LocationTypes.ShipHubs] = "SH",
            [LocationTypes.Showroom] = "SR",
            [LocationTypes.SalesOffice] = "SO",
            [LocationTypes.Warehouse] = "WH"
        };

        /// <summary>
        /// The list of Location Types for which BuyOnline attribute is applicable
        /// </summary>
        private static readonly IList<string> BuyOnlineValidLocationTypes = new List<string>
        {
            LocationTypes.Warehouse,
            LocationTypes.DistributionCenter,
            LocationTypes.MarketDistributionCenter,
            LocationTypes.AuxiliaryStorage,
            LocationTypes.ShipHubs
        };

        /// <summary>
        /// The list of Location Types for which 'Available to Storefront' attribute is applicable
        /// </summary>
        private static readonly IList<string> AvailableToStorefrontValidLocationTypes = new List<string>
        {
            LocationTypes.Warehouse,
            LocationTypes.DistributionCenter,
            LocationTypes.MarketDistributionCenter,
            LocationTypes.AuxiliaryStorage,
            LocationTypes.ShipHubs
        };

        /// <summary>
        /// The lov service
        /// </summary>
        private readonly ILovService _lovService;
        private readonly IPricingRegionService _pricingRegionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildLocationService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="changeHistoryService">The change history service.</param>
        /// <param name="calendarEventService">The calendar event service.</param>
        /// <param name="lovService">The lov service.</param>
        /// <param name="pricingRegionService">The pricing region service.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public ChildLocationService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig,
            IChangeHistoryService changeHistoryService, ICalendarEventService calendarEventService, ILovService lovService, IPricingRegionService pricingRegionService, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, changeHistoryService, appContextProvider, calendarEventService)
        {
            _lovService = lovService;
            _pricingRegionService = pricingRegionService;
        }

        public async Task<dynamic> CreateTestEvent(CalendarEventModel eventData, bool optimizePerformance, bool useMultithreading, int childLocCount = 0)
        {
            if (optimizePerformance)
            {
                var res = await CreateTestEventOptimized(eventData, childLocCount);
                return res;
            }
            else if (useMultithreading)
            {
                var res = await CreateTestEventMultithreading(eventData, childLocCount);
                return res;
            }
            else
            {
                var res = await CreateTestEventRegular(eventData, childLocCount);
                return res;
            }
        }

        public async Task<dynamic> CreateTestEventRegular(CalendarEventModel eventData, int childLocCount)
        {
            // 1. create Bulk event
            var createEventWatch = Stopwatch.StartNew();
            eventData.EventType = CalendarEventType.UnplannedBulk;
            var createdEventObj = await _calendarEventService.CreateAsync(eventData);
            var createdEventGuid = createdEventObj.Value<string>("eventGuid");
            createEventWatch.Stop();

            // 2. get child locations
            var loadChildLocsWatch = Stopwatch.StartNew();
            string whereQuery = $"where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter}";
            string orderBy = "order by c.node";
            string offsetAndLimit = childLocCount > 0 ? $"offset 0 limit {childLocCount}" : "";
            string sql = $"select * from c {whereQuery} {orderBy} {offsetAndLimit}";
            var items = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            loadChildLocsWatch.Stop();

            // 3. associate it with new event
            var updateChildLocsWatch = Stopwatch.StartNew();
            var newItems = new List<JObject>();
            foreach (var item in items)
            {
                // perform clone using audit trail
                JObject newItem = PrepareAndCloneObject(item);
                newItems.Add(newItem);

                // update calendar event guids
                var eventGuids = item.GetStringList("calendarEventGuid");
                eventGuids ??= new List<string>();
                eventGuids.Add(createdEventGuid);

                newItem["calendarEventGuid"] = JArray.FromObject(eventGuids);

                // update child location
                await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, item, newItem);
            }
            updateChildLocsWatch.Stop();

            // 4. create history for each 
            var addHistoryWatch = Stopwatch.StartNew();
            var addedEvents = new List<JObject> { createdEventObj };
            foreach (var newItem in newItems)
            {
                await _changeHistoryService.AddLocationEventsChangesAsync(NodeType.ChildLoc, newItem, addedEvents);
            }
            addHistoryWatch.Stop();

            // 5. return timings
            var res = new
            {
                childLocCount = items.Count,
                totalSummary = new
                {
                    createEventSec = Math.Round(createEventWatch.Elapsed.TotalSeconds, 3),
                    loadLocationsSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3),
                    updateLocationsSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3),
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3)
                },
                oneLocSummary = new
                {
                    loadLocationSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    updateLocationSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3) / items.Count
                }
            };

            return res;
        }

        public async Task<dynamic> CreateTestEventMultithreading(CalendarEventModel eventData, int childLocCount)
        {
            // 1. create Bulk event
            var createEventWatch = Stopwatch.StartNew();
            eventData.EventType = CalendarEventType.UnplannedBulk;
            var createdEventObj = await _calendarEventService.CreateAsync(eventData);
            var createdEventGuid = createdEventObj.Value<string>("eventGuid");
            createEventWatch.Stop();

            // 2. get child locations
            var loadChildLocsWatch = Stopwatch.StartNew();
            string whereQuery = $"where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter}";
            string orderBy = "order by c.node";
            string offsetAndLimit = childLocCount > 0 ? $"offset 0 limit {childLocCount}" : "";
            string sql = $"select * from c {whereQuery} {orderBy} {offsetAndLimit}";
            var items = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            loadChildLocsWatch.Stop();

            // 3. associate it with new event
            var updateChildLocsWatch = Stopwatch.StartNew();
            var newItems = new List<JObject>();
            var updateTasks = new List<Task>();
            foreach (var item in items)
            {
                // perform clone using audit trail
                JObject newItem = PrepareAndCloneObject(item);
                newItems.Add(newItem);

                // update calendar event guids
                var eventGuids = item.GetStringList("calendarEventGuid");
                eventGuids ??= new List<string>();
                eventGuids.Add(createdEventGuid);

                newItem["calendarEventGuid"] = JArray.FromObject(eventGuids);

                // update child location
                updateTasks.Add(UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, item, newItem));
            }
            await Task.WhenAll(updateTasks);
            updateChildLocsWatch.Stop();

            // 4. create history for each 
            var addHistoryWatch = Stopwatch.StartNew();
            var addedEvents = new List<JObject> { createdEventObj };
            var historyTasks = new List<Task>();
            foreach (var newItem in newItems)
            {
                historyTasks.Add(_changeHistoryService.AddLocationEventsChangesAsync(NodeType.ChildLoc, newItem, addedEvents));
            }
            await Task.WhenAll(historyTasks);
            addHistoryWatch.Stop();

            // 5. return timings
            var res = new
            {
                childLocCount = items.Count,
                totalSummary = new
                {
                    createEventSec = Math.Round(createEventWatch.Elapsed.TotalSeconds, 3),
                    loadLocationsSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3),
                    updateLocationsSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3),
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3)
                },
                oneLocSummary = new
                {
                    loadLocationSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    updateLocationSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3) / items.Count
                }
            };

            return res;
        }

        public async Task<dynamic> CreateTestEventOptimized(CalendarEventModel eventData, int childLocCount)
        {
            // 1. create Bulk event
            var createEventWatch = Stopwatch.StartNew();
            eventData.EventType = CalendarEventType.UnplannedBulk;
            var createdEventObj = await _calendarEventService.CreateAsync(eventData);
            var createdEventGuid = createdEventObj.Value<string>("eventGuid");
            createEventWatch.Stop();

            // 2. get child locations
            var loadChildLocsWatch = Stopwatch.StartNew();
            string whereQuery = $"where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter}";
            string orderBy = "order by c.node";
            string offsetAndLimit = childLocCount > 0 ? $"offset 0 limit {childLocCount}" : "";
            string sql = $"select * from c {whereQuery} {orderBy} {offsetAndLimit}";
            var items = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            loadChildLocsWatch.Stop();

            // 3. associate it with new event
            var updateChildLocsWatch = Stopwatch.StartNew();

            // create batch
            var batch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey));

            var newItems = new List<JObject>();
            foreach (var item in items)
            {
                // perform clone using audit trail
                JObject newItem = PrepareAndCloneObject(item);
                newItems.Add(newItem);

                // update calendar event guids
                var eventGuids = item.GetStringList("calendarEventGuid");
                eventGuids ??= new List<string>();
                eventGuids.Add(createdEventGuid);

                newItem["calendarEventGuid"] = JArray.FromObject(eventGuids);

                // update child location
                batch.UpdateItem(item, newItem);
            }
            // execute batches for all location types
            await batch.ExecuteAsync();
            updateChildLocsWatch.Stop();

            // 4. create history for each 
            var addHistoryWatch = Stopwatch.StartNew();
            var historyTimings = await _changeHistoryService.AddHistoryForChildLocBulkEventAsync(newItems, createdEventObj);
            addHistoryWatch.Stop();

            // 5. return timings
            var res = new
            {
                childLocCount = items.Count,
                totalSummary = new
                {
                    createEventSec = Math.Round(createEventWatch.Elapsed.TotalSeconds, 3),
                    loadLocationsSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3),
                    updateLocationsSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3),
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3)
                },
                oneLocSummary = new
                {
                    loadLocationSec = Math.Round(loadChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    updateLocationSec = Math.Round(updateChildLocsWatch.Elapsed.TotalSeconds, 3) / items.Count,
                    addHistorySec = Math.Round(addHistoryWatch.Elapsed.TotalSeconds, 3) / items.Count
                },
                historySummary = historyTimings
            };

            return res;
        }



        #region CRUDS

        /// <summary>
        /// Searches the child locations matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// The macthed child locations.
        /// </returns>
        public async Task<SearchResult<ChildLocationSummaryModel>> SearchAsync(LocationSearchCriteria criteria)
        {
            string[] propsToRetrieve =
                {
                    "c.node as id",
                    "c.locationName as name",
                    "c.financialData",
                    "c.address",
                    "c.locationType",
                    "c.districtName",
                    "c.pricingRegion as pricingRegions",
                    "c.regionName",
                    "c.kob",
                    "c.partition_key as partitionKey",
                    "ARRAY_SLICE(c.associate, 0, 5) AS associate",
                };

            string propList = string.Join(", ", propsToRetrieve);

            // filter by partition (Campus/Region/ChildLoc)
            string partitionKeyFilter = $"c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'";
            if (criteria.IncludeCampus)
            {
                partitionKeyFilter += $" or c.partition_key='{CosmosConfig.CampusPartitionKey}'";
            }
            if (criteria.IncludeRegion)
            {
                partitionKeyFilter += $" or c.partition_key='{CosmosConfig.RegionPartitionKey}'";
            }

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where ({partitionKeyFilter}) {ActiveRecordFilter}");

            // construct location name/ID criteria. Should check against node, locationName, and locationID (which can either be array or single value)
            string location = criteria.Location?.Trim();
            if (!string.IsNullOrEmpty(location))
            {
                // check 'node'
                string condition = $"CONTAINS(c.node, '{location}', true)";

                // check 'locationName'
                condition += $"OR CONTAINS(c.locationName, '{location}', true)";

                // check 'locationID' for childLoc partition key
                condition += $"OR (c.partition_key='childLoc' and CONTAINS(c.locationID, '{location}', true))";

                // check 'locationID' for campus/region partition keys
                condition += $"OR ((c.partition_key='campus' or c.partition_key='region') and EXISTS (SELECT VALUE locID from locID in c.locationID WHERE CONTAINS(locID, '{location}', true)))";

                // append to query
                whereQuery.Append($" and ({condition})");
            }

            whereQuery
                .AppendContainsCondition("address.addressLine1", criteria.AddressLine1)
                .AppendContainsCondition("address.addressLine2", criteria.AddressLine2)
                .AppendContainsCondition("address.addressLine3", criteria.AddressLine3)
                .AppendContainsCondition("address.cityName", criteria.City)
                .AppendContainsCondition("address.state", criteria.State)
                .AppendContainsCondition("address.countryName", criteria.CountryName)
                .AppendEqualsCondition("address.postalCodePrimary", criteria.ZipCode)
                .AppendEqualsCondition("address.postalCodeExtn", criteria.ZipCodeExtn)
                .AppendContainsCondition("regionName", criteria.Region)
                .AppendContainsOrCondition("glbuID", "glbuName", criteria.Glbu)
                .AppendEqualsCondition("locationType", criteria.LocationType)
                .AppendContainsCondition("kob", criteria.Kob)
                .AppendEqualsCondition("districtID", criteria.District);

            // add Pricing Region filter (NOTE: simple value for Child Loc and array for Campus/Region)
            if (!string.IsNullOrWhiteSpace(criteria.PricingRegion))
            {
                string trimmedCriteria = criteria.PricingRegion.Trim();
                // note: use commented out for 'contains' option
                //string childLocCondition = $"(c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and CONTAINS(c.pricingRegion, '{trimmedCriteria}', true))";
                string pricingRegionFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE fd.pricingRegion='{trimmedCriteria}')";
                string childLocCondition = $"(c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and {pricingRegionFilter})";
                string campusAndRegionCondition = $"(IS_DEFINED(c.pricingRegion)=true and ARRAY_CONTAINS(c.pricingRegion, '{trimmedCriteria}', true))";
                whereQuery.Append($" and ({childLocCondition} or {campusAndRegionCondition})");
            }

            // LOB CC Id/Name filter should use sub-query to check 'financialData' array
            if (!string.IsNullOrWhiteSpace(criteria.LobCc))
            {
                string lobCcIdFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE CONTAINS(fd.lobCc, '{criteria.LobCc.Trim()}', true))";
                string lobCcNameFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE CONTAINS(fd.lobCcName, '{criteria.LobCc.Trim()}', true))";

                whereQuery.Append($" AND ({lobCcIdFilter} or {lobCcNameFilter})");
            }

            // CostCenterId filter should use sub-query to check 'financialData' array
            if (!string.IsNullOrWhiteSpace(criteria.CostCenterId))
            {
                string costCenterIdFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE CONTAINS(fd.costCenterId, '{criteria.CostCenterId.Trim()}', true))";
                whereQuery.Append($" AND {costCenterIdFilter}");
            }

            // Child Location Nodes
            string[] locationNodes = Util.SplitMultivalueCriteria(criteria.LocationNodes);
            whereQuery.AppendContainsCondition("node", locationNodes);

            // CostCenterId filter should use sub-query to check 'financialData' array
            string[] costCenterIds = Util.SplitMultivalueCriteria(criteria.CostCenterIds);
            if (costCenterIds?.Length > 0)
            {
                string valuesCsv = costCenterIds.Select(x => x.ToUpperInvariant()).StringJoin("','");
                string costCenterIdFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE fd.costCenterId IN('{valuesCsv}'))";
                whereQuery.Append($" AND {costCenterIdFilter}");
            }

            string joinStatement = "";

            // append associate name filter
            if (!string.IsNullOrWhiteSpace(criteria.AssociateName))
            {
                string associateName = criteria.AssociateName.Trim();
                joinStatement = "JOIN a IN c.associate";
                whereQuery.Append($"and(CONTAINS(CONCAT(a.firstName, ' ', a.lastName), '{associateName}', true) OR CONTAINS(CONCAT(a.firstName, ' ', a.middleName, ' ', a.lastName), '{associateName}', true))");
            }

            // specify Order By
            string orderBy = string.Empty;
            if (!string.IsNullOrWhiteSpace(criteria.SortBy))
            {
                string sortByProp = GetSortByPropName(criteria.SortBy.Split(':').First<string>());
                string direction = criteria.SortBy.Split(':').Length > 1 ? criteria.SortBy.Split(':').Last<string>() : "ASC";
                orderBy = $"ORDER BY c.{sortByProp} {direction.ToUpper()}";
            }

            var result = new SearchResult<ChildLocationSummaryModel>();

            // construct query to get page results
            int offset = (criteria.PageNum - 1) * criteria.PageSize;
            string sql = $"select {propList} from c {joinStatement} {whereQuery} {orderBy} offset {offset} limit {criteria.PageSize}";

            // get page items
            result.Items = await GetAllItemsAsync<ChildLocationSummaryModel>(CosmosConfig.LocationsContainerName, sql);

            // set contacts and Pricing Region
            foreach (var item in result.Items)
            {
                item.Contacts = item.Associate.ToTitledContacts();

                // for Child locations retrieve Pricing Regions from Fin Data
                if (item.PartitionKey == CosmosConfig.ChildLocationPartitionKey)
                {
                    if (item.FinancialData != null)
                    {
                        item.PricingRegions = item.FinancialData.Select(x => x.PricingRegion).ToList();
                    }
                }

                if (item.PricingRegions != null)
                {
                    item.PricingRegion = item.PricingRegions.StringJoin(", ");
                }
            }

            // get total Count
            string totalCountSql = $"select value count(1) from c {joinStatement} {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets the child location by Id.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        /// <returns>The child location details.</returns>
        public async Task<ChildLocationDetailsModel> GetAsync(string node)
        {
            // TODO: remove fields that belong to financial data
            string[] propsToRetrieve =
                {
                    "c.node",
                    "c.dbaName",
                    "c.address",
                    "c.financialData",
                    "c.productOffering",
                    "c.brands",
                    "c.professionalAssociations",
                    "c.merchandisingVideos",
                    "c.merchandisingBanners",
                    "c.branchAdditionalContent",
                    "c.valueAddedServices",
                    "c.servicesAndCertification as servicesCertification",
                    "c.locationID",
                    "c.locationName",
                    "c.locationType",
                    "c.edmcsLocationName",
                    "c.regionNodeId",
                    "c.glbuName", // TODO: is needed?
                    "c.districtName", // TODO: is needed?
                    "c.regionID",
                    "c.regionName",
                    "c.locationLandingPageURL as landingPageUrl", // TODO: is needed?
                    "c.locationPhoneNumber as locationPhoneNumbers",
                    "c.customerFacingLocationName",
                    "c.domLocationStatus", // TODO: is needed?
                    "c.latitude",
                    "c.longitude",
                    "c.calendarEventGuid",
                    "c.kob",
                    "c.pricingRegion",
                    "c.vanityKobPhoneNumber",
                    "c.vanityKobFaxNumber",
                    "c.mainBranchNumber",
                    "LOWER(c.proPickup)='y' as proPickup",
                    "LOWER(c.dayLightSavingFlag)='y' as dayLightSavings",
                    "c.proPickupDuration",
                    "{" +
                    "  openForBusiness: LOWER(c.openforBusinessFlag)='y'," +
                    "  openForPurchase: LOWER(c.openforPurchaseFlag)='y'," +
                    "  openForShipping: LOWER(c.openforShipping)='y'," +
                    "  openForReceiving: LOWER(c.openforReceiving)='y'," +
                    "  proPickup: LOWER(c.proPickup)='y'," +
                    "  selfCheckout: LOWER(c.selfCheckout)='y'," +
                    "  bopis: LOWER(c.bopis)='y'," +
                    "  buyOnline: LOWER(c.buyOnline)='y'," +
                    "  availableToStorefront: LOWER(c.availableToStorefront)='y'," +
                    "  textToCounter: LOWER(c.textToCounter)='y'," +
                    "  textToCounterPhoneNumber: c.textToCounterPhoneNumber," +
                    "  staffPropickup: LOWER(c.staffUnStaffProPickup)='y'," +
                    "  openForDisclosure: LOWER(c.openforPublicDiscloure)='y'," +
                    "  lockerPU: c.lockerPU," +
                    "  cages: c.cages" +
                    "} as businessInfo",
                    "c.visibletoWebsitesFlag",
                    "c.operatingHours as operatingHoursSource",
                    "ARRAY_SLICE(c.associate, 0, 5) AS associate",
                    "c.recEffDt as lastUpdatedOn",
                    "c.timeZoneIdentifier"
                };

            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{node}' {ActiveRecordFilter}";
            var jObj = await LoadChildLocationAsync(node, sql);

            var model = jObj.ToObject<ChildLocationDetailsModel>();

            // set Contacts
            model.Contacts = model.Associate.ToTitledContacts();

            // load calendar events
            model.CalendarEvents = await LoadEventsAsync(NodeType.ChildLoc, node, jObj);

            // get operating hours
            model.OperatingHours = CastOperatingHours(jObj["operatingHoursSource"] as JArray);

            // set Visible To Website
            var jArray = jObj["visibletoWebsitesFlag"] as JArray;
            if (jArray?.Count > 0)
            {
                model.BusinessInfo.VisibleToWebsite = jArray[0]["visible"].FromShortYesNo();
            }

            // set Campus Node Id
            var regionNodeId = jObj.Value<string>("regionNodeId");
            model.CampusNodeId = await GetCampusIdByRegionIdAsync(regionNodeId);

            // set Locker PU display name
            if (!string.IsNullOrWhiteSpace(model.BusinessInfo.LockerPU))
            {
                var lockerPUs = await _lovService.GetAllValuesAsync<LovLookupModel>("lockerPU");
                model.BusinessInfo.LockerPUDisplayName = lockerPUs.FirstOrDefault(x => x.Value == model.BusinessInfo.LockerPU)?.DisplayName;
            }

            // set Cages display name
            if (!string.IsNullOrWhiteSpace(model.BusinessInfo.Cages))
            {
                var cagesLov = await _lovService.GetAllValuesAsync<LovLookupModel>("cages");
                model.BusinessInfo.CagesDisplayName = cagesLov.FirstOrDefault(x => x.Value == model.BusinessInfo.Cages)?.DisplayName;
            }

            // get Branch Additional Content Defaults (in case it is not exists yet)
            // TODO: remove in case all existing Child Locations will be populated with this data
            if (model.BranchAdditionalContent == null)
            {
                model.BranchAdditionalContent = await GetDefaultBranchAdditionalContentAsync(model.Kob);
            }

            return model;
        }

        /// <summary>
        /// Gets status of the child location with given Id.
        /// </summary>
        /// <param name="childLocId">The child location identifier.</param>
        /// <returns>
        /// The child location status.
        /// </returns>
        public async Task<RecordStatus> GetStatusAsync(string childLocId)
        {
            string sql =
                $"SELECT value {{ statusType: c.recStatus, statusDate: c.recEffDt }} FROM c" +
                $" where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{childLocId}'" +
                $" order by c.recEffDt desc {FirstOnlyFilter}";

            var recordStatus = await GetValueAsync<RecordStatus>(CosmosConfig.LocationsContainerName, sql, CosmosConfig.ChildLocationPartitionKey);
            if (recordStatus == null)
            {
                recordStatus = new RecordStatus
                {
                    StatusType = RecordStatusType.NotExists
                };
            }

            recordStatus.Id = childLocId;
            return recordStatus;
        }

        /// <summary>
        /// Creates the Child Location.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created child location details.</returns>
        public async Task<string> CreateAsync(ChildLocationCreateModel model)
        {
            Util.ValidateLocationTypeKOB(model.LocationType, model.Kob);

            var regionObj = await LoadRegionInfoAsync(model.RegionNodeId);
            var regionInfo = regionObj.ToObject<RegionDetailsModel>();

            var locationObj = await GetChildLocationAttributesFromEdmcsAsync(model.LocationId, model.LocationType, regionInfo.RegionId, regionInfo.Address);

            // set Id and user provided data
            string nextId = await GetChildLocationNextIdAsync(model.LocationType);
            locationObj["node"] = nextId;
            locationObj["locationName"] = model.LocationName;
            locationObj["locationType"] = model.LocationType;
            locationObj["kob"] = model.Kob;
            locationObj["customerFacingLocationName"] = model.CustomerFacingLocationName;
            locationObj["regionNodeId"] = model.RegionNodeId;
            locationObj["associate"] = new JArray();

            // set default business info values
            SetDefaultBusinessInfoValues(locationObj);

            // set default merchandising videos
            SetDefaultMerchandisingVideos(locationObj, model.Kob);

            // set defaults for Branch Additional Content
            await SetDefaultBranchAdditionalContentAsync(locationObj, model.Kob);

            // set BOPIS based on KOB
            SetBopisFlag(locationObj, model.Kob);

            // set flags default values
            locationObj["textToCounter"] = false.ToShortYesNo();
            locationObj["buyOnline"] = false.ToShortYesNo();
            locationObj["availableToStorefront"] = false.ToShortYesNo();

            // set Pricing Region (if financial data set)
            await SetPricingRegionAsync(locationObj);

            var jArray = new JArray();
            var jObject = new JObject
            {
                ["visible"] = "N"
            };
            jArray.Add(jObject);
            locationObj["visibletoWebsitesFlag"] = jArray;

            locationObj["dayLightSavingFlag"] = "N";

            // add operating hours
            var operatingHours = new JArray();
            for (int i = 0; i < 7; i++)
            {
                var hoursObj = new JObject
                {
                    ["day"] = ((DayOfWeek) i).ToString(),
                    ["openHour"] = "",
                    ["closeHour"] = "",
                    ["openAfterHour"] = "",
                    ["closeAfterHour"] = "",
                    ["openReceivingHour"] = "",
                    ["closeReceivingHour"] = "",
                    ["openCloseFlag"] = "Closed"
                };
                operatingHours.Add(hoursObj);
            }
            locationObj["operatingHours"] = operatingHours;

            // inherited
            locationObj["timeZoneIdentifier"] = regionInfo.TimezoneIdentifier;
            locationObj["openforBusinessFlag"] = regionInfo.BusinessInfo.OpenForBusiness.ToShortYesNo();
            locationObj["openforPublicDiscloure"] = regionInfo.BusinessInfo.OpenForDisclosure.ToShortYesNo();
            locationObj["latitude"] = regionInfo.Latitude;
            locationObj["longitude"] = regionInfo.Longitude;

            // set events (inherited + upcoming Planned)
            var parentEventGuis = regionObj.GetStringList("calendarEventGuid");
            await SetEventsForNewChildLocation(locationObj, parentEventGuis);

            // set Branch Page URL (for Customer-facing locations only)
            SetBranchPageUrl(locationObj);

            // set Branch Page URL Prefix & Suffix
            SetBranchPageUrlPrefixAndSuffix(locationObj);

            // set internal props
            PrepareNewObjectProperties(locationObj, CosmosConfig.ChildLocationPartitionKey);

            // create location
            await CreateItemAsync(locationObj, CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey);

            // create change history for Created child location
            await _changeHistoryService.AddChildLocationCreatedChangesAsync(nextId, locationObj);

            // return created location's Id
            return nextId;
        }

        /// <summary>
        /// Sets the pricing region asynchronous.
        /// </summary>
        /// <param name="locationObj">The location object.</param>
        private async Task SetPricingRegionAsync(JObject locationObj)
        {
            var financialData = locationObj.GetObjectList("financialData");
            foreach (var item in financialData)
            {
                var trilogieLogon = item.Value<string>("trilogieLogon");
                if (trilogieLogon != null)
                {
                    var pricingRegion = await _pricingRegionService.GetPricingRegionAsync(trilogieLogon);
                    item["pricingRegion"] = pricingRegion;
                }
                else
                {
                    // when no mapping found, set empty value
                    item["pricingRegion"] = null;
                }
            }
        }

        /// <summary>
        /// Adds the planned events for new child location.
        /// </summary>
        /// <param name="childLoc">The child location.</param>
        /// <param name="parentEventGuids">The parent event guids.</param>
        private async Task SetEventsForNewChildLocation(JObject childLoc, IList<string> parentEventGuids)
        {
            string locationNode = childLoc.Value<string>("node");
            string locationName = childLoc.Value<string>("locationName");

            // this will contain final list of event guids (inherited + upcomnig planned)
            var allEventGuids = parentEventGuids.ToList();

            var inheritedEventIds = await _calendarEventService.GetEventIdsAsync(parentEventGuids);

            var templates = await _calendarEventService.GetPlannedEventTemplatesAsync(excludePastEvents: true);
            foreach (var item in templates)
            {
                // skip if such event already inherited from parent Region
                if (inheritedEventIds.Contains(item.EventId))
                {
                    continue;
                }

                var createdEvent = await _calendarEventService.CreateAsync(new CalendarEventModel
                {
                    EventType = CalendarEventType.Planned,
                    EventId = item.EventId,
                    EventName = item.EventName,
                    EventDescription = item.EventDescription,
                    DisplayDuration = item.DisplayDuration,
                    EventStartDay = item.EventStartDay,
                    EventStartTime = item.EventStartTime,
                    EventEndDay = item.EventEndDay,
                    EventEndTime = item.EventEndTime,
                    Closure = item.Closure,
                    DisplaySequence = item.DisplaySequence,
                    EventDuration = item.EventDuration,
                    LocationName = locationName,
                    LocationNode = locationNode
                });

                var createdEventGuid = createdEvent.Value<string>("eventGuid");
                allEventGuids.Add(createdEventGuid);
            }

            // update location's event list
            childLoc["calendarEventGuid"] = new JArray(allEventGuids);
        }

        /// <summary>
        /// Updates the Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Child Location data.</param>
        public async Task UpdateAsync(string node, ChildLocationPatchModel model)
        {
            // NOTE: [Future] mode change region to separate API endpoint
            // check if 'region' is being updated
            if (!string.IsNullOrEmpty(model.RegionNodeId))
            {
                await MoveToRegionAsync(node, model.RegionNodeId);
            }
            // check if 'normal' update
            else
            {
                // remove duplicates
                model.PhoneNumbers = model.PhoneNumbers?.Distinct()?.ToList();

                // load Child Location
                var jObj = await LoadChildLocationAsync(node);

                // perform clone using audit trail
                JObject newObj = PrepareAndCloneObject(jObj);

                var isLatitudeEmpty = model.Latitude == null;
                var isLongitudeEmpty = model.Longitude == null;
                if (!isLatitudeEmpty && isLongitudeEmpty)
                {
                    throw new ArgumentException($"Longitude cannot be empty if Latitude is present");
                }
                if (!isLongitudeEmpty && isLatitudeEmpty)
                {
                    throw new ArgumentException($"Latitude cannot be empty if Longitude is present");
                }

                // set updated values
                if (model.Address != null)
                {
                    JObject addressObj = newObj["address"] as JObject;
                    addressObj.UpdateOptionalProperty("addressLine2", model.Address.AddressLine2);
                    addressObj.UpdateOptionalProperty("addressLine3", model.Address.AddressLine3);
                    addressObj.UpdateOptionalProperty("altShiptoAddressLine2", model.Address.AltShiptoAddressLine2);
                    addressObj.UpdateOptionalProperty("altShiptoAddressLine3", model.Address.AltShiptoAddressLine3);
                }

                newObj.UpdateOptionalProperty("customerFacingLocationName", model.CustomerFacingLocationName);
                newObj.UpdateOptionalProperty("dbaName", model.DbaName);
                newObj.UpdateOptionalProperty("locationName", model.LocationName);
                newObj.UpdateOptionalProperty("productOffering", model.ProductOffering);
                newObj.UpdateOptionalProperty("longitude", model.Longitude);
                newObj.UpdateOptionalProperty("latitude", model.Latitude);
                newObj.UpdateOptionalProperty("locationPhoneNumber", model.PhoneNumbers);
                newObj.UpdateOptionalProperty("servicesAndCertification", model.ServicesCertification);
                newObj.UpdateOptionalProperty("valueAddedServices", model.ValueAddedServices);
                newObj.UpdateOptionalProperty("domLocationStatus", model.DomLocationStatus);
                newObj.UpdateOptionalProperty("locationLandingPageURL", model.LocationLandingPageURL);
                newObj.UpdateOptionalProperty("vanityKobPhoneNumber", model.VanityKobPhoneNumber);
                newObj.UpdateOptionalProperty("vanityKobFaxNumber", model.VanityKobFaxNumber);
                newObj.UpdateOptionalProperty("vanityKobPhoneNumber", model.VanityKobPhoneNumber);
                newObj.UpdateOptionalProperty("mainBranchNumber", model.MainBranchNumber);

                // Text to Counter Phone Number (has a little business logic)
                newObj.UpdateOptionalProperty("textToCounterPhoneNumber", model.TextToCounterPhoneNumber);
                if (model.TextToCounterPhoneNumber != null && model.TextToCounterPhoneNumber.Trim().Length == 0)
                {
                    // this is the case when Text to Counter Phone Number is cleared, so we need to turn off the Text to Counter flag
                    newObj.UpdateProperty("textToCounter", false.ToShortYesNo());
                }

                // update item (using Audit Trail and Optimistic Concurrency)
                await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

                // create change history/summary
                await _changeHistoryService.AddChildLocationChangesAsync(jObj, newObj);
            }
        }

        /// <summary>
        /// Moves the Child Location to a different Region.
        /// </summary>
        /// <param name="node">The Child Location node.</param>
        /// <param name="newRegionNode">The new Region node.</param>
        public async Task MoveToRegionAsync(string node, string newRegionNode)
        {
            // load Child Location
            var jObj = await LoadChildLocationAsync(node);

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            // load RegionId, RegionName
            string sql = $"select c.regionID, c.locationName, c.calendarEventGuid from c where c.partition_key='{CosmosConfig.RegionPartitionKey}'" +
                $" and c.node='{newRegionNode}' {ActiveRecordFilter}";
            var regionObj = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            var regionInfo = regionObj.ToObject<RegionDetailsModel>();

            // make sure Region with provided Id exists
            if (regionInfo == null)
            {
                throw new EntityNotFoundException($"Region with node='{newRegionNode}' was not found.");
            }

            // make sure new Region is under the same Campus
            var currentCampusId = await GetCampusIdByRegionIdAsync(jObj.Value<string>("regionNodeId"));
            var newCampusId = await GetCampusIdByRegionIdAsync(newRegionNode);

            if (currentCampusId != newCampusId)
            {
                throw new DataConflictException($"New region '{newRegionNode}' should be under the same Campus '{currentCampusId}'.");
            }

            // update Child Location's region info 
            newObj.UpdateOptionalProperty("regionNodeId", newRegionNode);
            newObj.UpdateOptionalProperty("regionID", regionInfo.RegionId);
            newObj.UpdateOptionalProperty("regionName", regionInfo.LocationName);
            newObj.UpdateOptionalProperty("edmcsNode", newRegionNode);

            // set new list of events (remove old inherited, and new inherited)
            var selfEventGuids = await LoadLocationSelfEventGuids(node);
            var parentEventGuids = regionObj.GetStringList("calendarEventGuid");
            var allEvents = selfEventGuids.Concat(parentEventGuids);
            newObj.UpdateProperty("calendarEventGuid", allEvents);

            // remove all Financial Data from moved Child Location
            var emptyFinancialData = new List<FinancialDataItem>();
            newObj.UpdateOptionalProperty("financialData", emptyFinancialData);

            // set Branch Page URL based on updated values
            SetBranchPageUrl(newObj);

            // update Pricing Region
            await SetPricingRegionAsync(newObj);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(jObj, newObj);
        }

        /// <summary>
        /// Deletes the child location by Id.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        public async Task DeleteAsync(string node)
        {
            // load Campus
            var jObj = await LoadChildLocationAsync(node);

            // perform clone using audit trail
            JObject newObj = PrepareDeleteAndCloneObject(jObj);

            // set business info values N when child location is deleted
            SetDefaultBusinessInfoValues(newObj);

            // set BOPIS based on KOB
            bool bopisFlag = newObj["kob"].ToString() == KOBs.Plumbing;
            if(bopisFlag) newObj["bopis"] = false.ToShortYesNo();

            // set flags default values
            bool textToCounterFlag = newObj["textToCounter"].ToString() == true.ToShortYesNo();
            if (textToCounterFlag)
                newObj["textToCounterPhoneNumber"] = string.Empty;
            newObj["textToCounter"] = false.ToShortYesNo();
            var jArray = new JArray();
            var jObject = new JObject
                {
                    ["visible"] = false.ToShortYesNo()
                };
            jArray.Add(jObject);
            newObj["visibletoWebsitesFlag"] = jArray;

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

            // delete related children
            await _calendarEventService.DeleteLocationEventsAsync(node);

            // create change history for Deleted child location
            await _changeHistoryService.AddChildLocationDeletedChangesAsync(node, newObj);
        }

        #endregion


        #region Partial Update

        /// <summary>
        /// Updates the Brands values of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Brands values.</param>
        public async Task UpdateBrandsAsync(string node, IList<string> model)
        {
            // load Child Location
            var childLocObj = await LoadChildLocationAsync(node);

            // check if value has actually changed
            var existingValue = childLocObj.GetStringList("brands");
            if (existingValue.SequenceEqual(model))
            {
                // skip update if value didn't change
                return;
            }

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(childLocObj);

            newObj.UpdateOptionalProperty("brands", model);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, childLocObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(childLocObj, newObj);
        }

        /// <summary>
        /// Updates the Professional Associations of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Professional Associations.</param>
        public async Task UpdateProfessionalAssociationsAsync(string node, IList<ProfessionalAssociationModel> items)
        {
            // load Child Location
            var childLocObj = await LoadChildLocationAsync(node);

            var newItems = items.OrderBy(x => x.Sequence).ToList();

            var existingValue = childLocObj["professionalAssociations"];
            var existingItems = existingValue != null
                ? existingValue.ToObject<IList<ProfessionalAssociationModel>>()
                : new List<ProfessionalAssociationModel>();

            // check if value has actually changed
            if (existingItems.Count == newItems.Count)
            {
                bool hasChanges = false;
                foreach (var item in existingItems)
                {
                    var newItem = newItems.Find(x => x.ItemGuid == item.ItemGuid);
                    if (newItem == null)
                    {
                        hasChanges = true;
                        break;
                    }
                    if (newItem.Name != item.Name ||
                        newItem.LogoSource != item.LogoSource ||
                        newItem.Url != item.Url ||
                        newItem.Sequence != item.Sequence)
                    {
                        hasChanges = true;
                        break;
                    }
                }

                if (!hasChanges)
                {
                    // skip update if value didn't change
                    return;
                }
            }

            // set Guids for new items
            newItems
                .Where(x => string.IsNullOrWhiteSpace(x.ItemGuid))
                .ForEach(x => x.ItemGuid = Guid.NewGuid().ToString());

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(childLocObj);
            newObj.UpdateOptionalProperty("professionalAssociations", newItems);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, childLocObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationProfessionalAssociatesChangesAsync(childLocObj, existingItems, items);
        }

        /// <summary>
        /// Updates the Merchandising Banners of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Merchandising Banners.</param>
        public async Task UpdateMerchandisingBannersAsync(string node, IList<MerchandisingBannerModel> items)
        {
            // load Child Location
            var childLocObj = await LoadChildLocationAsync(node);

            var newItems = items.OrderBy(x => x.Sequence).ToList();

            var existingValue = childLocObj["merchandisingBanners"];
            var existingItems = existingValue != null
                ? existingValue.ToObject<IList<MerchandisingBannerModel>>()
                : new List<MerchandisingBannerModel>();

            // check if value has actually changed
            if (existingItems.Count == newItems.Count)
            {
                bool hasChanges = false;
                foreach (var item in existingItems)
                {
                    var newItem = newItems.Find(x => x.ItemGuid == item.ItemGuid);
                    if (newItem == null)
                    {
                        hasChanges = true;
                        break;
                    }

                    if (newItem.Name != item.Name ||
                        newItem.ImageName != item.ImageName ||
                        newItem.StartsOn != item.StartsOn ||
                        newItem.EndsOn != item.EndsOn ||
                        newItem.LongText != item.LongText ||
                        newItem.ReferringCid != item.ReferringCid ||
                        newItem.ReferringDomain != item.ReferringDomain ||
                        newItem.Sequence != item.Sequence ||
                        newItem.GroupSequence != item.GroupSequence ||
                        newItem.Time != item.Time ||
                        newItem.TypeName != item.TypeName ||
                        newItem.SupportingDocument != item.SupportingDocument)
                    {
                        hasChanges = true;
                        break;
                    }
                }

                if (!hasChanges)
                {
                    // skip update if value didn't change
                    return;
                }
            }

            // set Guids for new items
            newItems
                .Where(x => string.IsNullOrWhiteSpace(x.ItemGuid))
                .ForEach(x => x.ItemGuid = Guid.NewGuid().ToString());

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(childLocObj);
            newObj.UpdateOptionalProperty("merchandisingBanners", newItems);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, childLocObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationMerchandisingBannersChangesAsync(childLocObj, existingItems, items);
        }

        /// <summary>
        /// Updates the Branch Additional Content of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="data">The updated Branch Additional Content.</param>
        public async Task UpdateBranchAdditionalContentAsync(string node, ChildBranchAdditionalContentModel data)
        {
            // load Child Location
            var childLocObj = await LoadChildLocationAsync(node);

            var existingItem = childLocObj["branchAdditionalContent"]?.ToObject<ChildBranchAdditionalContentModel>();
            if (existingItem == null)
            {
                // TODO: is it needed in case we update all existing Child Locs in DB?
                // load default values, if not exist yet
                string kob = childLocObj.Value<string>("kob");
                existingItem = await GetDefaultBranchAdditionalContentAsync(kob);
            }
            var locationType = childLocObj["locationType"];
            // prevent updating readonly fields
            data.BusinessGroupImageNames = existingItem.BusinessGroupImageNames;
            data.BranchPageUrl = existingItem.BranchPageUrl;
            if((string)locationType != "Showroom")
                data.AppointmentUrl = existingItem.AppointmentUrl;
            data.UrlPrefix = existingItem.UrlPrefix;
            data.UrlSuffix = existingItem.UrlSuffix;

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(childLocObj);
            newObj.UpdateProperty("branchAdditionalContent", data);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, childLocObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(childLocObj, newObj);
        }

        /// <summary>
        /// Updates the Financial Data items of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Financial Data items.</param>
        public async Task UpdateFinancialDataAsync(string node, IList<FinancialDataItem> model)
        {
            // check for duplicates
            int distinctCount = model.Select(i => i.CostCenterId).Distinct().Count();
            bool hasDuplicates = distinctCount < model.Count;
            if (hasDuplicates)
            {
                throw new PersistenceException("duplicated cost center id");
            }

            // load Child Location
            var jObj = await LoadChildLocationAsync(node);

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            var newFinancialData = new List<FinancialDataItem>();
            foreach (var item in model)
            {
                if (!string.IsNullOrEmpty(item.CostCenterId) || !string.IsNullOrEmpty(item.LobCc))
                {
                    string locationId = newObj.Value<string>("locationID");
                    string regionId = newObj.Value<string>("regionID");
                    var address = newObj["address"].ToObject<NodeAddress>();

                    FinancialDataItem financialData = await FindEdmcsFinancialDataAsync(locationId, regionId, address, item.CostCenterId, item.LobCc);
                    if (financialData == null)
                    {
                        throw new EntityNotFoundException($"Cost Center with Id='{item.CostCenterId}' was not found.");
                    }

                    newFinancialData.Add(financialData);
                }
            }

            newObj.UpdateOptionalProperty("financialData", newFinancialData);

            // set Branch Page URL based on updated values
            SetBranchPageUrl(newObj);

            // update Pricing Region
            await SetPricingRegionAsync(newObj);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(jObj, newObj);
        }

        /// <summary>
        /// Updates the Business Info of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Business Info data.</param>
        /// <returns></returns>
        public async Task UpdateBusinessInfoAsync(string node, ChildLocBusinessInfoModel model)
        {
            // load Child Location
            var jObj = await LoadChildLocationAsync(node);

            SetDefaultBusinessInfoValues(jObj);

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            if (model.StaffPropickup == true)
            {
                // Staff Pro-Pickup should also enable Pro-Pickup
                model.ProPickup = true;
            }

            // when 'Text To Counter' is OFF, the 'Text To Counter Phone Number' must be null/empty
            if (model.TextToCounter == false)
            {
                // this is the case when Text to Counter flag switched off, just clear the phone number value
                // (do not set null, otherwise it won't be updated due to Patch logic)
                model.TextToCounterPhoneNumber = string.Empty;
            }

            // apply BuyOnline rules
            if (model.BuyOnline == true)
            {
                // BuyOnline can only be true for specific Location Types
                var locType = jObj.LocationType();
                if (!BuyOnlineValidLocationTypes.Contains(locType))
                {
                    model.BuyOnline = false;
                }

                // when BuyOnline is true, some other flags must be set to true as well
                model.OpenForBusiness = true;
                model.OpenForPurchase = true;
                model.OpenForDisclosure = true;
                model.OpenForReceiving = true;
            }

            // apply 'Available To Storefront' rules
            if (model.AvailableToStorefront == true)
            {
                // AvailableToStorefront can only be true for specific Location Types
                var locType = jObj.LocationType();
                if (!AvailableToStorefrontValidLocationTypes.Contains(locType))
                {
                    model.AvailableToStorefront = false;
                }
            }

            // set updated values
            newObj.UpdateProperty("openforBusinessFlag", model.OpenForBusiness.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("openforPurchaseFlag", model.OpenForPurchase.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("openforShipping", model.OpenForShipping.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("openforReceiving", model.OpenForReceiving.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("proPickup", model.ProPickup.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("selfCheckout", model.SelfCheckout.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("bopis", model.Bopis.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("buyOnline", model.BuyOnline.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("availableToStorefront", model.AvailableToStorefront.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("textToCounter", model.TextToCounter.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("textToCounterPhoneNumber", model.TextToCounterPhoneNumber);
            newObj.UpdateProperty("lockerPU", model.LockerPU);
            newObj.UpdateProperty("cages", model.Cages);

            var proPickup = newObj.Value<string>("proPickup") == "Y";
            if (!proPickup)
            {
                // Staff Pro-Pickup and Pro-Pickup Duration can only be enabled when Pro-Pickup is enabled
                model.StaffPropickup = false;
                newObj["proPickupDuration"] = null;
            }

            newObj.UpdateProperty("staffUnStaffProPickup", model.StaffPropickup.ToShortYesNo(emptyIfNull: false));
            newObj.UpdateProperty("openforPublicDiscloure", model.OpenForDisclosure.ToShortYesNo(emptyIfNull: false));

            // set Visible To Website
            if (model.VisibleToWebsite != null)
            {
                var jArray = newObj["visibletoWebsitesFlag"] as JArray;
                if (jArray.Count > 0)
                {
                    jArray[0]["visible"] = model.VisibleToWebsite.ToShortYesNo();
                }
                else
                {
                    var jObject = new JObject
                    {
                        ["visible"] = model.VisibleToWebsite.ToShortYesNo()
                    };
                    jArray.Add(jObject);
                }
            }


            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(jObj, newObj);
        }

        /// <summary>
        /// Updates the Operating Hours of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Operating Hours data.</param>
        public async Task UpdateOperatingHoursAsync(string node, OperatingHoursUpdateModel model)
        {
            await CheckChildLocationExistsAsync(node);

            // adjust values - change NULL to ""
            model.OperatingHours.ForEach(x =>
            {
                x.OpenHour ??= "";
                x.CloseHour ??= "";
                x.OpenAfterHour ??= "";
                x.CloseAfterHour ??= "";
                x.OpenReceivingHour ??= "";
                x.CloseReceivingHour ??= "";
            });

            // get locations which should be updated with the given data
            IList<JObject> locationsToUpdate = await GetLocationToUpdate(node, model.ApplyToChildren);

            // create batch of location updates
            TransactionalBatch batch = CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey);
            foreach (var existingObj in locationsToUpdate)
            {
                // perform clone using audit trail
                JToken newObj = PrepareAndCloneObject(existingObj);

                // set updated hours
                var hoursArray = new JArray();
                foreach (var item in model.OperatingHours)
                {
                    item.DayOfWeekName = ((DayOfWeek)item.DayOfWeek).ToString();
                    var hoursObj = new JObject
                    {
                        ["day"] = ((DayOfWeek)item.DayOfWeek).ToString(),
                        ["openHour"] = item.OpenHour,
                        ["closeHour"] = item.CloseHour,
                        ["openAfterHour"] = item.OpenAfterHour,
                        ["closeAfterHour"] = item.CloseAfterHour,
                        ["openReceivingHour"] = item.OpenReceivingHour,
                        ["closeReceivingHour"] = item.CloseReceivingHour,
                        ["openCloseFlag"] = item.OpenCloseFlag
                    };
                    hoursArray.Add(hoursObj);
                }

                // set updated values
                newObj["operatingHours"] = hoursArray;

                // update Pro-Pickup duration
                if (model.ProPickupDuration != null)
                {
                    //string newDurationValue = model.ProPickupDuration;
                    var proPickup = newObj.Value<string>("proPickup") == "Y";
                    if (!proPickup)
                    {
                        // Staff Pro-Pickup can only be enabled when Pro-Pickup is enabled
                        model.ProPickupDuration = null;
                    }

                    newObj["proPickupDuration"] = model.ProPickupDuration;
                }

                // process Update Existing and Create New items to batch list
                batch
                    .ReplaceItemWithConcurrency(existingObj)
                    .CreateItem(newObj);
            }

            // update all child locations in single transaction
            await ExecuteBatchAsync(batch);

            // save changes for all updated locations
            foreach (var location in locationsToUpdate)
            {
                JObject oldObj = new JObject
                {
                    ["node"] = location["node"],
                    ["locationName"] = location["locationName"],
                    ["address"] = location["address"],
                    ["proPickupDuration"] = location["proPickupDuration"]
                };
                JObject newObj = new JObject
                {
                    ["node"] = location["node"],
                    ["locationName"] = location["locationName"],
                    ["address"] = location["address"],
                    ["proPickupDuration"] = model.ProPickupDuration
                };

                await _changeHistoryService.AddChildLocationChangesAsync(oldObj, newObj);
                await _changeHistoryService.AddChildLocationOperatingHoursChangesAsync(location, model.OperatingHours);
            }
        }

        /// <summary>
        /// Assigns or Unassigns Associates with the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Id.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        public async Task AssignAssociatesAsync(string node, AssignAssociatesModel model, AssignMode assignMode)
        {
            // load Child Location
            var jObj = await LoadChildLocationAsync(node);

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
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, jObj, newObj);

            // save associates changes
            await _changeHistoryService.AddChildLocationAssociatesChangesAsync(newObj, oldAssociates, newAssociates);
        }

        /// <summary>
        /// Updates Location Type and KOB for the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Node.</param>
        /// <param name="model">The updated location type details.</param>
        /// <returns>Node of the updated Child Location.</returns>
        public async Task<string> UpdateLocationTypeAsync(string node, UpdateLocationTypeModel model)
        {
            // load Child Location
            var childLocObj = await LoadChildLocationAsync(node);

            var existingLocationType = childLocObj.LocationType();
            if (existingLocationType == model.LocationType)
            {
                // skip in case Location Type wasn't changed
                return node;
            }

            bool isCustomerFacing = LocationTypes.CustomerFacingLocationTypes.Contains(existingLocationType);
            bool isUpdatedCustomerFacing = LocationTypes.CustomerFacingLocationTypes.Contains(model.LocationType);
            
            // validate Customer Facing
            if (isCustomerFacing)
            {
                if (!isUpdatedCustomerFacing)
                {
                    throw new ArgumentException("Customer Facing Location can only be changed to Customer Facing Location Type.");
                }

                // validate KOB
                Util.ValidateLocationTypeKOB(model.LocationType, model.Kob);
            }
            // validate Inventory
            else
            {
                // force change KOB to null for Inventory location
                model.Kob = null;

                if (isUpdatedCustomerFacing)
                {
                    throw new ArgumentException("Inventory Location can only be changed to Inventory Location Type.");
                }
            }

            var oldNode = childLocObj.Node();

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(childLocObj);

            // set business info values N for old Child Location LocationType
            SetDefaultBusinessInfoValues(childLocObj);

            // set BOPIS based on KOB
            bool bopisFlag = childLocObj["kob"].ToString() == KOBs.Plumbing;
            if (bopisFlag) childLocObj["bopis"] = false.ToShortYesNo();

            // set flags default values
            bool textToCounterFlag = childLocObj["textToCounter"].ToString() == true.ToShortYesNo();
            if (textToCounterFlag)
                childLocObj["textToCounterPhoneNumber"] = string.Empty;
            childLocObj["textToCounter"] = false.ToShortYesNo();
            var jArray = new JArray();
            var jObject = new JObject
            {
                ["visible"] = false.ToShortYesNo()
            };
            jArray.Add(jObject);
            childLocObj["visibletoWebsitesFlag"] = jArray;


            // update provided attributes
            newObj.UpdateProperty("locationType", model.LocationType);
            newObj.UpdateProperty("kob", model.Kob);

            //Update CustomerFacingLocationName
            if (isCustomerFacing)
            {
                // set Customer Facing Location Name
                // expected format: [CITY], [STATE] [ZIP CODE] – [Ferguson  KOB]
                JObject address = (JObject)newObj["address"];
                var value = $"{address["cityName"].ToString().ToUpper()}, {address["state"].ToString().ToUpper()} " +
                    $"{address["postalCodePrimary"].ToString().ToUpper()} - Ferguson {(string)newObj["kob"]}";
                newObj.UpdateProperty("customerFacingLocationName", value);
            }

            // get new Child Loc ID (node) for the updated Location Type
            var newNode = await GetChildLocationNextIdAsync(model.LocationType);
            newObj.UpdateProperty("node", newNode); 
            newObj.UpdateProperty("prevNode", oldNode);

            // update properties that depend on KOB (in case KOB changed)
            if (model.Kob != childLocObj.KOB())
            {
                // updated Business Group Image Names in Branch Additional Content (specific to new KOB)
                await SetBusinessGroupImageNamesAsync(newObj, model.Kob);

                // update Merchandising Videos
                SetDefaultMerchandisingVideos(newObj, model.Kob);

                // update urlPrefix and urlSuffix
                SetBranchPageUrlPrefixAndSuffix(newObj);

                // update Branch Page URL
                SetBranchPageUrl(newObj);

                // update BOPIS flag
                SetBopisFlag(newObj, model.Kob);
            }

            //update AppointmentUrl when locationType is changed from Showroom to other Customer Facing Location.
            SetBranchPageAppointmentUrl(newObj);

            //update recStatus = Deleted for Old Location Type.
            childLocObj.UpdateProperty("recStatus", "Deleted");

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey, childLocObj, newObj);

            // change Node in Change History/Summary
            await _changeHistoryService.ChangeObjectIdAsync(oldNode, newNode);

            // create change history/summary
            await _changeHistoryService.AddChildLocationChangesAsync(childLocObj, newObj);

            return newNode;
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Checks the child location exists.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        private async Task CheckChildLocationExistsAsync(string node)
        {
            string sql = $"select value Count(1) from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'" +
                $" and c.node='{node}' {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, sql);
            if (count == 0)
            {
                throw new EntityNotFoundException($"Child LocationId with Node '{node}' was not found.");
            }
        }

        /// <summary>
        /// Gets the Campus location name by Campus node Id.
        /// </summary>
        /// <param name="campusNodeId">The Campus node Id.</param>
        /// <returns>The Campus location name.</returns>
        private async Task<string> GetCampusLocationName(string campusNodeId)
        {
            var sql = $"select value c.locationName from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node='{campusNodeId}' {ActiveRecordFilter}";
            var locationName = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return locationName;
        }

        /// <summary>
        /// Gets the location to update.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        /// <param name="includeLocationsFromSameRegion">Determines whether locations within the same Region should be included.</param>
        /// <returns>The location to update.</returns>
        private async Task<IList<JObject>> GetLocationToUpdate(string node, bool includeLocationsFromSameRegion)
        {
            // construct location filter depending on whether children needs to be updated
            string locationFilter;
            if (includeLocationsFromSameRegion)
            {
                // load all locations within the same Region
                string regionId = await GetRegionIdByChildLocationNode(node);
                locationFilter = $"c.regionNodeId='{regionId}'";
            }
            else
            {
                locationFilter = $"c.node='{node}'";
            }

            // load Child Locations
            string sql = $"select * from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and {locationFilter} {ActiveRecordFilter}";
            var locationsToUpdate = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return locationsToUpdate;
        }

        /// <summary>
        /// Gets the EDMCS Child Location.
        /// </summary>
        /// <param name="locationId">The Location ID.</param>
        /// <param name="locationType">Type of the location.</param>
        /// <param name="regionId">The region Id.</param>
        /// <param name="address">The address.</param>
        /// <returns>
        /// The EDMCS Child Location.
        /// </returns>
        private async Task<JObject> GetChildLocationAttributesFromEdmcsAsync(string locationId, string locationType, string regionId, NodePrimaryAddress address)
        {
            string[] propsToRetrieve =
            {
                "c.regionID",
                "c.regionName",
                "c.campusNodeId",
                "c.locationID",
                "c.locationName as edmcsLocationName",
                "c.address"
            };

            string propList = string.Join(", ", propsToRetrieve);
            string city = address.CityName.Replace("'", "\\'");
            Console.WriteLine("City = " + city);
            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.EdmcsMasterPartitionKey}' {ActiveRecordFilter}");
            whereQuery
                .AppendEqualsCondition("locationID", locationId)
                .AppendEqualsCondition("regionID", regionId)
                .AppendEqualsCondition("address.state", address.State)
                .AppendEqualsCondition("address.cityName", city)
                .AppendEqualsCondition("address.postalCodePrimary", address.PostalCodePrimary)
                .AppendEqualsCondition("address.addressLine1", address.AddressLine1);

            string sql = $"select {propList} from c {whereQuery}";

            // get location details
            var result = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            if (result == null)
            {
                throw new EntityNotFoundException("Child Location with given Location ID, Region, and Address wasn't found.");
            }

            // set financial data to empty list
            result.UpdateProperty("financialData", new List<FinancialDataItem>());

            return result;
        }

        private async Task<JObject> LoadRegionInfoAsync(string regionNode)
        {
            string[] propsToRetrieve =
                {
                    "c.regionID as regionId",
                    "c.timeZoneIdentifier as timezoneIdentifier",
                    "{" +
                    "  openForBusiness: LOWER(c.openforBusinessFlag)='y'," +
                    "  openForDisclosure: LOWER(c.openforPublicDiscloure)='y'" +
                    "} as businessInfo",
                    "c.latitude",
                    "c.longitude",
                    "c.campusNodeId",
                    "c.address",
                    "c.calendarEventGuid"
                };

            var result = await LoadRegionObjAsync(regionNode, propsToRetrieve);
            return result;
        }

        /// <summary>
        /// Casts the operating hours from persistence to model format.
        /// </summary>
        /// <param name="sourceArray">The source array.</param>
        /// <returns>The operating hours.</returns>
        private static IList<OperatingHoursModel> CastOperatingHours(JArray sourceArray)
        {
            var result = new List<OperatingHoursModel>();
            if (sourceArray == null)
            {
                return result;
            }

            foreach (var sourceItem in sourceArray)
            {
                string dayOfWeek = sourceItem.Value<string>("day");

                var item = new OperatingHoursModel
                {
                    DayOfWeek = (int)Enum.Parse<DayOfWeek>(dayOfWeek),
                    OpenHour = sourceItem.Value<string>("openHour"),
                    CloseHour = sourceItem.Value<string>("closeHour"),
                    OpenAfterHour = sourceItem.Value<string>("openAfterHour"),
                    CloseAfterHour = sourceItem.Value<string>("closeAfterHour"),
                    OpenReceivingHour = sourceItem.Value<string>("openReceivingHour"),
                    CloseReceivingHour = sourceItem.Value<string>("closeReceivingHour"),
                    OpenCloseFlag = sourceItem.Value<string>("openCloseFlag"),
                };

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Gets the child location next Id.
        /// </summary>
        /// <param name="locationType">Type of the location.</param>
        /// <returns>Next Id.</returns>
        private async Task<string> GetChildLocationNextIdAsync(string locationType)
        {
            if (!LocationTypePrefixes.TryGetValue(locationType, out string idPrefix))
            {
                throw new ArgumentException($"'{locationType}' is not a valid Location Type.");
            }

            string where = $"WHERE c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' AND c.locationType='{locationType}' AND STARTSWITH(c.node, '{idPrefix}')";
            string sql = $"SELECT VALUE MAX(c.node) FROM c {where}";

            string maxId = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);

            int idNumber = maxId != null
                ? Convert.ToInt32(maxId.Substring(idPrefix.Length))
                : 0;

            var nextIdNumber = idNumber + 1;
            string nextId = $"{idPrefix}{nextIdNumber:D8}";
            return nextId;
        }

        /// <summary>
        /// Gets the sort by property.
        /// </summary>
        /// <param name="sortBy">The sort by.</param>
        /// <returns>The Sort By property name</returns>
        private static string GetSortByPropName(string sortBy)
        {
            return sortBy switch
            {
                "locationId" => "node",
                "address" => "address.addressLine1",
                "state" => "address.state",
                "region" => "regionName",
                "glbu" => "glbuName",
                _ => sortBy,
            };
        }

        /// <summary>
        /// Sets the default business information values.
        /// </summary>
        /// <param name="jObj">The child location object.</param>
        private static void SetDefaultBusinessInfoValues(JObject jObj)
        {
            // set default values to "N" no matter what value exists in the attribute
            jObj["openforBusinessFlag"] = false.ToShortYesNo();
            jObj["openforPurchaseFlag"] = false.ToShortYesNo();
            jObj["openforShipping"] = false.ToShortYesNo();
            jObj["openforReceiving"] = false.ToShortYesNo();
            jObj["proPickup"] = false.ToShortYesNo();
            jObj["selfCheckout"] = false.ToShortYesNo();
            jObj["staffUnStaffProPickup"] = false.ToShortYesNo();
            jObj["openforPublicDiscloure"] = false.ToShortYesNo();
            jObj["buyOnline"] = false.ToShortYesNo();
            jObj["availableToStorefront"] = false.ToShortYesNo();
        }

        /// <summary>
        /// Sets the default business information values.
        /// </summary>
        /// <param name="jObj">The child location object.</param>
        /// <param name="kob">The KOB value.</param>
        private static void SetDefaultMerchandisingVideos(JObject jObj, string kob)
        {
            // default values should only be added for Showroom KOB (for now)
            if (kob != LocationTypes.Showroom)
            {
                jObj.Remove("merchandisingVideos");
                return;
            }

            // default value (make configurable in future?)
            var defaultValue = new List<MerchandisingVideoModel>
            {
                new MerchandisingVideoModel
                {
                    ItemGuid = Guid.NewGuid().ToString(),
                    Name = "197904097",
                    Description = "Showroom Experience",
                    Sequence = 2,
                    GroupSequence = 1
                }
            };

            jObj.UpdateProperty("merchandisingVideos", defaultValue);
        }

        /// <summary>
        /// Sets the default business information values.
        /// </summary>
        /// <param name="childLocObj">The child location object.</param>
        /// <param name="kob">The KOB value.</param>
        private async Task SetDefaultBranchAdditionalContentAsync(JObject childLocObj, string kob)
        {
            var defaultItem = await GetDefaultBranchAdditionalContentAsync(kob);
            childLocObj.UpdateProperty("branchAdditionalContent", defaultItem);
        }

        private async Task<ChildBranchAdditionalContentModel> GetDefaultBranchAdditionalContentAsync(string kob)
        {
            // get LOV of default values for each KOB (DB filtering is not needed, because it is stored as array in the json doc)
            var defaultItems = await _lovService.GetAllValuesAsync<BranchAdditionalContentDefaultsLovModel>("branchAdditionalContentDefaults");
            var kobItem = defaultItems.FirstOrDefault(x => x.Kob == kob);
            return new ChildBranchAdditionalContentModel
            {
                BusinessGroupImageNames = kobItem?.BusinessGroupImageNames
            };
        }

        private static void SetBranchPageUrl(JObject childLocObj)
        {
            if (childLocObj.IsCustomerFacingLocation())
            {
                // get Branch Additional Content data
                var branchContentObj = childLocObj.BranchAdditionalContent();

                // calculate and set Branch Page URL
                string branchPageUrl = BranchPageUrlCalculator.Calculate(childLocObj);
                branchContentObj.UpdateProperty("branchPageUrl", branchPageUrl);
            }
        }

        private static void SetBranchPageUrlPrefixAndSuffix(JObject childLocObj)
        {
            if (childLocObj.IsCustomerFacingLocation())
            {
                // get Branch Additional Content data
                var branchContentObj = childLocObj.BranchAdditionalContent();

                // calculate and set Branch Page URL Prefix
                string urlPrefix = BranchPageUrlCalculator.CalculateUrlPrefix(childLocObj);
                branchContentObj.UpdateProperty("urlPrefix", urlPrefix);

                // calculate and set Branch Page URL Suffix
                string urlSuffix = BranchPageUrlCalculator.CalculateUrlSuffix(childLocObj);
                branchContentObj.UpdateProperty("urlSuffix", urlSuffix);
            }
        }
        private static void SetBranchPageAppointmentUrl(JObject childLocObj)
        {
            if (childLocObj.IsCustomerFacingLocation())
            {
                if(childLocObj.LocationType() != "Showroom")
                {
                    // get Branch Additional Content data
                    var branchContentObj = childLocObj.BranchAdditionalContent();
                    branchContentObj.UpdateProperty("appointmentUrl", string.Empty);
                }
            }
        }
        private async Task SetBusinessGroupImageNamesAsync(JObject childLocObj, string kob)
        {
            var defaultBranchAdditionalContent = await GetDefaultBranchAdditionalContentAsync(kob);
            var branchContentObj = childLocObj.BranchAdditionalContent();
            branchContentObj.UpdateProperty("businessGroupImageNames", defaultBranchAdditionalContent.BusinessGroupImageNames);
        }

        private static void SetBopisFlag(JObject childLoc, string kob)
        {
            // set BOPIS based on KOB
            bool bopisFlag = kob == KOBs.Plumbing;
            childLoc["bopis"] = bopisFlag.ToShortYesNo();
        }

        #endregion
    }
}
