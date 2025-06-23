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
    /// The Pricing Region service.
    /// </summary>
    public class PricingRegionService : BaseLocationService, IPricingRegionService
    {
        private static readonly IList<string> TrilogieLogonToExclude = new List<string>
        {
            "",
            "NA"
        };

        /// <summary>
        /// The lookup service
        /// </summary>
        private readonly ILookupService _lookupService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="changeHistoryService">The change history service.</param>
        /// <param name="lookupService">The lookup service.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public PricingRegionService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IChangeHistoryService changeHistoryService, ILookupService lookupService, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, changeHistoryService, appContextProvider)
        {
            _lookupService = lookupService;
        }

        #region Pricing Region Mapping

        /// <summary>
        /// Gets the Pricing Region Mappings.
        /// </summary>
        /// <returns>The Pricing Region Mappings.</returns>
        public async Task<IList<PricingRegionMappingModel>> GetPricingRegionMappingsAsync()
        {
            // get all Trilogie Logon values from EDMCS
            var allTrilogieLogonValues = await _lookupService.GetDistinctTrilogieLogonsFromEdmcsAsync();

            // exclude unwanted values
            allTrilogieLogonValues = allTrilogieLogonValues.Except(TrilogieLogonToExclude).ToList();

            // get Campus usage per trilogie logon in EDMCS
            var campusUsages = await GetTrilogieLogonUsageFromEdmcsAsync();
            // get Child Loc counts per each Trilogie Logon
            var childLocUsages = await GetTrilogieLogonUsagesAsync();

            // get existing mapping
            var existingMappings = await GetPricingRegionMappingItemsAsync();

            // initiate result items
            var result = new List<PricingRegionMappingModel>();

            // sort Trilogie Logon values
            var orderedValues = allTrilogieLogonValues.OrderBy(x => x);

            // populate statistics for each Trilogie Logon
            foreach (var trilogieLogon in orderedValues)
            {
                var mapping = new PricingRegionMappingModel();
                mapping.TrilogieLogon = trilogieLogon;

                // get all Campuses associated with the Trilogie Logon
                var trilogieLogonCampuses = campusUsages
                    .Where(x => x.TrilogieLogon == trilogieLogon)
                    .Select(x => x.CampusNode)
                    .Distinct();

                // set Campus count for Trilogie Logon
                mapping.CampusCount = trilogieLogonCampuses.Count();

                // set Child Location count for Trilogie Logon
                if (childLocUsages.TryGetValue(trilogieLogon, out var childLocCount))
                {
                    mapping.ChildLocCount = childLocCount;
                }

                // set data from existing mapping
                var existingMapping = existingMappings.FirstOrDefault(x => x.TrilogieLogon == trilogieLogon);
                if (existingMapping != null)
                {
                    mapping.PricingRegionId = existingMapping.PricingRegionId;
                    mapping.PricingRegion = existingMapping.PricingRegion;
                }

                result.Add(mapping);
            }

            return result;
        }

        /// <summary>
        /// Gets the Pricing Region by Trilogie Logon.
        /// </summary>
        /// <returns>The Pricing Region.</returns>
        public async Task<string> GetPricingRegionAsync(string trilogieLogon)
        {
            string whereExpr = $"where c.trilogieLogon='{trilogieLogon}' and c.partition_key='{CosmosConfig.PricingRegionMappingPartitionKey}' {ActiveRecordFilter}";
            string sql = $"SELECT value c.pricingRegion FROM c {whereExpr}";

            var result = await GetValueAsync<string>(CosmosConfig.ApplicationContainerName, sql);
            return result;
        }

        /// <summary>
        /// Creates new Pricing Region mapping.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created model Id.</returns>
        public async Task<PricingRegionMappingModel> CreateMappingAsync(PricingRegionMappingCreateModel model)
        {
            // set Id and user provided data
            string nextId = await GetPricingRegionNextIdAsync();

            var pricingRegionObj = new JObject
            {
                ["pricingRegionId"] = nextId,
                ["trilogieLogon"] = model.TrilogieLogon,
                ["pricingRegion"] = model.PricingRegion
            };

            // set internal props
            PrepareNewObjectProperties(pricingRegionObj, CosmosConfig.PricingRegionMappingPartitionKey);

            // create Pricing Region mapping
            await CreateItemAsync(pricingRegionObj, CosmosConfig.ApplicationContainerName, CosmosConfig.PricingRegionMappingPartitionKey);

            var createdModel = new PricingRegionMappingModel
            {
                PricingRegionId = nextId,
                TrilogieLogon = model.TrilogieLogon,
                PricingRegion = model.PricingRegion
            };

            // add change history for created Pricing Region mapping
            await _changeHistoryService.AddPricingRegionCreatedChangesAsync(createdModel);

            // update pricing region for all matched locations
            await SetPricingRegionToChildLocationsAsync(model.TrilogieLogon, model.PricingRegion);
            await SetPricingRegionToCampusesAndRegionsAsync(model.TrilogieLogon, model.PricingRegion);

            return createdModel;
        }

        /// <summary>
        /// Deletes the Pricing Region mapping by Id.
        /// </summary>
        /// <param name="pricingRegionId">The Pricing Region Id.</param>
        public async Task DeleteMappingAsync(string pricingRegionId)
        {
            // load Campus
            var jObj = await LoadPricingRegionMappingAsync(pricingRegionId);

            // perform clone using audit trail
            JObject newObj = PrepareDeleteAndCloneObject(jObj);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.ApplicationContainerName, CosmosConfig.PricingRegionMappingPartitionKey, jObj, newObj);

            string trilogieLogon = jObj.Value<string>("trilogieLogon");
            string pricingRegion = jObj.Value<string>("pricingRegion");

            // add history
            await _changeHistoryService.AddPricingRegionDeletedChangesAsync(new PricingRegionMappingModel
            {
                PricingRegionId = pricingRegionId,
                TrilogieLogon = trilogieLogon,
                PricingRegion = pricingRegion
            });

            // update pricing region for all matched locations (set empty)
            await SetPricingRegionToChildLocationsAsync(trilogieLogon, "");
            await SetPricingRegionToCampusesAndRegionsAsync(trilogieLogon, "", pricingRegion);
        }

        /// <summary>
        /// Updates the pricing region mapping.
        /// </summary>
        /// <param name="pricingRegionId">The pricing region Id.</param>
        /// <param name="model">The updated Pricing Region mapping data.</param>
        public async Task UpdateMappingAsync(string pricingRegionId, PricingRegionMappingCreateModel model)
        {
            // load Pricing Region Mapping
            var jObj = await LoadPricingRegionMappingAsync(pricingRegionId);
            string oldPricingRegion = jObj.Value<string>("pricingRegion");

            // perform clone using audit trail
            JObject newObj = PrepareAndCloneObject(jObj);

            // set updated values (Note: update of trilogie Logon is disabled for now)
            //newObj.UpdateProperty("trilogieLogon", model.TrilogieLogon);
            newObj.UpdateProperty("pricingRegion", model.PricingRegion);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.ApplicationContainerName, CosmosConfig.PricingRegionMappingPartitionKey, jObj, newObj);

            // add change history
            var oldModel = new PricingRegionMappingModel
            {
                PricingRegionId = pricingRegionId,
                //TrilogieLogon = jObj.Value<string>("trilogieLogon"),
                PricingRegion = oldPricingRegion
            };
            var newModel = new PricingRegionMappingModel
            {
                PricingRegionId = pricingRegionId,
                //TrilogieLogon = model.TrilogieLogon,
                PricingRegion = model.PricingRegion
            };
            await _changeHistoryService.AddPricingRegionUpdatedChangesAsync(oldModel, newModel);

            // update pricing region for all matched locations
            await SetPricingRegionToChildLocationsAsync(model.TrilogieLogon, model.PricingRegion);
            await SetPricingRegionToCampusesAndRegionsAsync(model.TrilogieLogon, model.PricingRegion, oldPricingRegion);
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Loads the pricing region.
        /// </summary>
        /// <param name="pricingRegionId">The pricing region Id.</param>
        /// <returns>Pricing region.</returns>
        private async Task<JObject> LoadPricingRegionMappingAsync(string pricingRegionId)
        {
            string whereExpr = $"where c.partition_key='{CosmosConfig.PricingRegionMappingPartitionKey}' and c.pricingRegionId='{pricingRegionId}' {ActiveRecordFilter}";
            string sql = $"select * from c {whereExpr}";

            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.ApplicationContainerName, sql);
            var jObj = queryResult.FirstOrDefault();
            if (jObj == null)
            {
                throw new EntityNotFoundException($"Pricing Region with Id '{pricingRegionId}' was not found.");
            }

            return jObj;
        }

        /// <summary>
        /// Sets the Pricing Region to locations whose Trilogie Logon matches 'trilogieLogon' parameter.
        /// </summary>
        /// <param name="trilogieLogon">The trilogie logon.</param>
        /// <param name="pricingRegion">The pricing region.</param>
        private async Task SetPricingRegionToChildLocationsAsync(string trilogieLogon, string pricingRegion)
        {
            // get all Child Locations with the given Trilogie Logon
            string trilogieLogonFilter = $"EXISTS(SELECT VALUE fd FROM fd IN c.financialData WHERE fd.trilogieLogon='{trilogieLogon}')";
            string whereExpr = $"where {trilogieLogonFilter} and c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter}";
            string sql = $"select * from c {whereExpr}";
            var childLocations = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);

            // list of all changed locations
            var changedLocs = new List<ChangedObject>();

            // create batch
            var batch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChildLocationPartitionKey));

            foreach (var locObj in childLocations)
            {
                // perform clone using audit trail
                JObject newLocObj = PrepareAndCloneObject(locObj);
                var finData = newLocObj["financialData"] as JArray;
                foreach (var item in finData)
                {
                    if (item.Value<string>("trilogieLogon") == trilogieLogon)
                    {
                        item.UpdateProperty("pricingRegion", pricingRegion);
                    }
                }

                // add Update Existing and Create New items to batch list
                batch.UpdateItem(locObj, newLocObj);

                changedLocs.Add(new ChangedObject
                {
                    OldObject = locObj,
                    NewObject = newLocObj
                });
            }

            if (changedLocs.Count > 0)
            {
                // execute batches for all location types
                await batch.ExecuteAsync();

                // create history
                await _changeHistoryService.AddLocationsPricingRegionChangeAsync(changedLocs);
            }
        }

        /// <summary>
        /// Sets the Pricing Region to locations whose Trilogie Logon matches 'trilogieLogon' parameter.
        /// </summary>
        /// <param name="trilogieLogon">The trilogie logon.</param>
        /// <param name="pricingRegion">The pricing region.</param>
        /// <param name="oldPricingRegion">The old pricing region.</param>
        private async Task SetPricingRegionToCampusesAndRegionsAsync(string trilogieLogon, string pricingRegion, string oldPricingRegion = null)
        {
            // get Campuses and Regions for the given Trilogie Logon in EDMCS
            var edmcsUsages = await GetTrilogieLogonUsageFromEdmcsAsync(trilogieLogon);
            var campusNodes = edmcsUsages.Select(x => x.CampusNode).Distinct().ToList();
            var campuses = await LoadCampusesAsync(campusNodes);
            var regionNodes = edmcsUsages.Select(x => x.RegionNode).Distinct().ToList();
            var regions = await LoadRegionsAsync(regionNodes);

            // combine all locations
            var matchedLocations = campuses.Concat(regions);

            // list of all changed locations
            var changedLocs = new List<ChangedObject>();

            // create batches for different location types
            var campusBatch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.CampusPartitionKey));
            var regionBatch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.RegionPartitionKey));

            foreach (var locObj in matchedLocations)
            {
                // perform clone using audit trail
                JObject newLocObj = PrepareAndCloneObject(locObj);

                // get existing Pricing Region values
                var prValues = newLocObj.GetStringList("pricingRegion");

                // optionally remove previous Pricing Region for the given Trilogie Logon (needed during update operation)
                prValues.Remove(oldPricingRegion);

                // add new value to the Pricing Region list
                if (!string.IsNullOrEmpty(pricingRegion))
                {
                    prValues.Add(pricingRegion);
                }

                // update property with the new values
                newLocObj.UpdateProperty("pricingRegion", prValues);

                string pk = locObj.Value<string>("partition_key");
                NodeType nodeType = Util.GetNodeType(CosmosConfig, pk);

                // choose batch by node type
                UnlimitedTransactionalBatch nodeBatch;
                if (nodeType == NodeType.Campus)
                {
                    nodeBatch = campusBatch;
                }
                else
                {
                    nodeBatch = regionBatch;
                }

                // process Update Existing and Create New items to batch list
                nodeBatch.UpdateItem(locObj, newLocObj);

                changedLocs.Add(new ChangedObject
                {
                    OldObject = locObj,
                    NewObject = newLocObj
                });
            }

            // execute batches for all location types
            await campusBatch.ExecuteAsync();
            await regionBatch.ExecuteAsync();

            // create history
            await _changeHistoryService.AddLocationsPricingRegionChangeAsync(changedLocs);
        }

        /// <summary>
        /// Gets the next Id for Pricing Region Mapping.
        /// </summary>
        /// <returns>Next Id.</returns>
        private async Task<string> GetPricingRegionNextIdAsync()
        {
            const string idPrefix = "PRM";
            string whereExpr = $"WHERE c.partition_key='{CosmosConfig.PricingRegionMappingPartitionKey}' AND STARTSWITH(c.pricingRegionId, '{idPrefix}')";
            string sql = $"SELECT VALUE MAX(c.pricingRegionId) FROM c {whereExpr}";

            string maxId = await GetValueAsync<string>(CosmosConfig.ApplicationContainerName, sql);

            int idNumber = maxId != null
                ? Convert.ToInt32(maxId.Substring(idPrefix.Length))
                : 0;

            var nextIdNumber = idNumber + 1;
            string nextId = $"{idPrefix}{nextIdNumber:D7}";
            return nextId;
        }

        /// <summary>
        /// Gets the Trilogie Logon usages for the given partition key.
        /// </summary>
        /// <returns>Trilogie Logon usages.</returns>
        private async Task<IDictionary<string, int>> GetTrilogieLogonUsagesAsync()
        {
            string sql = $"SELECT t.trilogieLogon, count(1) as count"
                + " FROM c"
                + " JOIN t IN c.financialData"
                + $" WHERE LENGTH(t.trilogieLogon)>0 AND c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter}"
                + " GROUP BY t.trilogieLogon";

            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            var result = queryResult.ToDictionary(x => x.Value<string>("trilogieLogon"), x => x.Value<int>("count"));
            return result;
        }

        /// <summary>
        /// Gets the Trilogie Logon usages from the EDMCS data.
        /// </summary>
        /// <param name="trilogieLogon">The optional filter by trilogie logon.</param>
        /// <returns>Trilogie Logon usages.</returns>
        private async Task<IList<TrilogieLogonEdmcsUsageModel>> GetTrilogieLogonUsageFromEdmcsAsync(string trilogieLogon = null)
        {
            // build query
            string propList = "c.trilogieLogon, c.campusNodeId as campusNode, c.regionNodeId as regionNode";
            string whereExpr = $"where c.partition_key='{CosmosConfig.EdmcsMasterPartitionKey}' and IS_DEFINED(c.trilogieLogon)=true {ActiveRecordFilter}";
            if (trilogieLogon != null)
            {
                whereExpr += $" and c.trilogieLogon='{trilogieLogon}'";
            }

            string sql = $"SELECT {propList} FROM c {whereExpr}";

            // execute query
            var result = await GetAllItemsAsync<TrilogieLogonEdmcsUsageModel>(CosmosConfig.LocationsContainerName, sql);
            result = result.Where(x => !string.IsNullOrWhiteSpace(x.TrilogieLogon)).ToList();
            return result;
        }

        /// <summary>
        /// Gets the pricing region mapping items.
        /// </summary>
        /// <returns></returns>
        private async Task<IList<PricingRegionMappingModel>> GetPricingRegionMappingItemsAsync()
        {
            string[] propsToRetrieve =
                {
                    "c.pricingRegionId",
                    "c.trilogieLogon",
                    "c.pricingRegion"
                };

            var propList = string.Join(", ", propsToRetrieve);
            string whereExpr = $"where c.partition_key='{CosmosConfig.PricingRegionMappingPartitionKey}' {ActiveRecordFilter}";
            string sql = $"SELECT {propList} FROM c {whereExpr}";

            var result = await GetAllItemsAsync<PricingRegionMappingModel>(CosmosConfig.ApplicationContainerName, sql);
            return result;
        }

        #endregion

    }
}
