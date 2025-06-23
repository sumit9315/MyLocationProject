using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The base service for all Cosmos services.
    /// </summary>
    public abstract class BaseCosmosService
    {
        /// <summary>
        /// The hierarchy node field set.
        /// </summary>
        protected const string HierarchyNodeFieldSet = "c.node as id, c.locationName as name, c.locationType, c.partition_key as nodeType";

        /// <summary>
        /// The active record SQL filter.
        /// </summary>
        protected const string ActiveRecordFilter = "and c.recStatus='Active'";

        /// <summary>
        /// The active record SQL filter.
        /// </summary>
        protected const string ActiveOrDeletedRecordFilter = "and (c.recStatus='Active' OR c.recStatus='Deleted')";

        /// <summary>
        /// The first only filter.
        /// </summary>
        protected const string FirstOnlyFilter = "OFFSET 0 LIMIT 1";

        // Primary Financial Data attributes
        protected static readonly IList<string> _financialDataPrimaryProps = new List<string>
        {
            "costCenterID",
            "lobCc",
            "lobCcName",
            "locationEffectiveDate as lobCcEffDate",
            "locationCloseDate as lobCcCloseDate",
            "locationStatus = 'O' ? 'Open' : c.locationStatus = 'P' ? 'Pending' : 'Close' as lobCcStatus",
            "ccType",
            "oracleCompanyID",
            "oracleCompanyName",
            "einFederal",
            "inventoryOrg",
            "salesSystem",
            "trilogieLogon",
        };

        // Secondary Financial Data attributes
        protected static readonly IList<string> _financialDataSecondaryProps = new List<string>
        {
            "node as edmcsNode",
            "areaID",
            "areaName",
            "bmi",
            "ccoEmplId",
            "costOrg",
            "counter",
            "cpbuID",
            "cpbuDescription",
            "currency",
            "districtID",
            "districtName",
            "glbuID",
            "glbuName",
            "inspyrusOU",
            "intercoSales",
            "legacyDistrictID",
            "legacyDistrictName",
            "lobId",
            "lobCcOwnerName",
            "lobDescription",
            "notes",
            "showroom",
            "trilogieAlias",
            "workdayCompanyID"
        };

        /// <summary>
        /// The Cosmos DB client.
        /// </summary>
        protected readonly CosmosClient _cosmosClient;

        /// <summary>
        /// The Cosmos DB configuration.
        /// </summary>
        private readonly IOptions<CosmosConfig> _cosmosConfig;

        /// <summary>
        /// The application context provider.
        /// </summary>
        protected readonly IAppContextProvider _appContextProvider;

        /// <summary>
        /// Gets the Cosmos DB configuration.
        /// </summary>
        /// <value>
        /// The Cosmos DB configuration.
        /// </value>
        protected CosmosConfig CosmosConfig
        {
            get
            {
                return _cosmosConfig.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCosmosService"/> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        protected BaseCosmosService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig)
        {
            _cosmosClient = cosmosClient;
            _cosmosConfig = cosmosConfig;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCosmosService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        protected BaseCosmosService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IAppContextProvider appContextProvider)
            : this(cosmosClient, cosmosConfig)
        {
            _appContextProvider = appContextProvider;
        }

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <returns>the container</returns>
        protected Container GetContainer(string containerName)
        {
            return _cosmosClient.GetContainer(_cosmosConfig.Value.DbName, containerName);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <typeparam name="T">Type of items.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="callerName">Name of the caller.</param>
        /// <returns>
        /// The items from the query.
        /// </returns>
        protected async Task<IList<T>> GetAllItemsAsync<T>(string containerName, string sql, string partitionKey = null, [CallerMemberName] string callerName = "")
        {
            var container = GetContainer(containerName);

            var requestOptions = partitionKey != null
                ? new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) }
                : null;

            var iterator = container.GetItemQueryIterator<T>(sql, requestOptions: requestOptions);

            var allItems = new List<T>();
            decimal totalCharge = 0;
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                totalCharge += (decimal)page.RequestCharge;
                allItems.AddRange(page);
            }

            System.Diagnostics.Trace.WriteLine($"{callerName}: {totalCharge}");
            return allItems;
        }

        /// <summary>
        /// Gets first matched item, or null if not found.
        /// </summary>
        /// <typeparam name="T">Type of items.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>The matched item, or null if not found.</returns>
        protected async Task<T> GetFirstOrDefaultAsync<T>(string containerName, string sql, string partitionKey = null)
        {
            var allItems = await GetAllItemsAsync<T>(containerName, sql, partitionKey);
            return allItems.FirstOrDefault();
        }

        /// <summary>
        /// Gets the scalar value.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>the scalar value</returns>
        protected async Task<T> GetValueAsync<T>(string containerName, string sql, string partitionKey = null)
        {
            var items = await GetAllItemsAsync<T>(containerName, sql, partitionKey);
            return items.FirstOrDefault();
        }

        ///// <summary>
        ///// Gets the scalar value.
        ///// </summary>
        ///// <typeparam name="T">Type of the value.</typeparam>
        ///// <param name="containerName">Name of the container.</param>
        ///// <param name="sql">The SQL query.</param>
        ///// <param name="partitionKey">The partition key.</param>
        ///// <returns>the scalar value</returns>
        //protected async Task<IList<T>> GetValueListAsync<T>(string containerName, string sql, string partitionKey = null)
        //{
        //    var items = await GetAllItemsAsync<T>(containerName, sql, partitionKey);
        //    return items;
        //}

        /// <summary>
        /// Gets the list of scalar values.
        /// </summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>The scalar values</returns>
        protected async Task<IList<T>> GetValuesAsync<T>(string containerName, string sql, string partitionKey = null)
        {
            var items = await GetAllItemsAsync<T>(containerName, sql, partitionKey);
            return items;
        }

        #region Audit Trail, Batch & Concurrency handling

        /// <summary>
        /// Prepares the existing object for update and returns cloned object to create.
        /// </summary>
        /// <param name="existingObj">The existing object.</param>
        /// <returns>New object.</returns>
        protected JObject PrepareAndCloneObject(JObject existingObj)
        {
            var now = DateTime.UtcNow;

            // create new object by copying existing
            var newObj = existingObj.DeepClone() as JObject;

            // set previous object audit fields
            existingObj["recStatus"] = "Inactive";
            existingObj["recExpDt"] = now;

            // set new object audit fields
            newObj["recEffDt"] = now;
            newObj["lastUpdatedBy"] = _appContextProvider.GetCurrentUserFullName();
            newObj["id"] = Guid.NewGuid().ToString();
            return newObj;
        }

        /// <summary>
        /// Prepares the existing object for delete and returns cloned object to create.
        /// </summary>
        /// <param name="existingObj">The existing object.</param>
        /// <returns>New object.</returns>
        protected JObject PrepareDeleteAndCloneObject(JObject existingObj)
        {
            var now = DateTime.UtcNow;
            var newObj = PrepareAndCloneObject(existingObj);

            // set new object's status to Deleted
            newObj["recStatus"] = "Deleted";
            newObj["recExpDt"] = now;
            newObj["lastUpdatedBy"] = _appContextProvider.GetCurrentUserFullName();

            return newObj;
        }

        /// <summary>
        /// Updates the item in single transaction using Audit Trail and Optimistic Concurrency.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="existingObj">The existing object.</param>
        /// <param name="newObj">The new object.</param>
        protected async Task UpdateItemAsync(string containerName, string partitionKey, JToken existingObj, JToken newObj)
        {
            // update existing and create new in single transaction
            var batch = CreateTransactionalBatch(containerName, partitionKey)
                .ReplaceItemWithConcurrency(existingObj)
                .CreateItem(newObj);

            await ExecuteBatchAsync(batch);
        }

        /// <summary>
        /// Creates an item.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="items">The list of items to create.</param>
        protected async Task CreateItemFromListAsync<T>(string containerName, string partitionKey, IList<T> items)
        {
            // update existing and create new in single transaction
            var batch = CreateTransactionalBatch(containerName, partitionKey);
            foreach (T item in items)
            {
                JObject jObj = PrepareNewObjectProperties(item, partitionKey, setRecFields: false);
                batch.CreateItem(jObj);
            }

            await ExecuteBatchAsync(batch);
        }

        /// <summary>
        /// Upserts the item in single transaction using Audit Trail and Optimistic Concurrency.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="item">The item.</param>
        protected async Task UpsertItemAsync<T>(string containerName, string partitionKey, T item)
        {
            var container = GetContainer(containerName);
            await container.UpsertItemAsync(item, new PartitionKey(partitionKey));
        }

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="item">The item.</param>
        /// <param name="setRecFields">A flag indicating whether recXxx metadata properties should be set.</param>
        protected async Task<JObject> CreateItemAsync<T>(string containerName, string partitionKey, T item, bool setRecFields = true)
        {
            JObject jObj = PrepareNewObjectProperties(item, partitionKey, setRecFields);
            await CreateItemAsync(jObj, containerName, partitionKey);
            return jObj;
        }

        /// <summary>
        /// Creates the object in the given container.
        /// </summary>
        /// <param name="jObj">The object.</param>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        protected async Task CreateItemAsync(JObject jObj, string containerName, string partitionKey)
        {
            var container = GetContainer(containerName);
            await container.CreateItemAsync(jObj, new PartitionKey(partitionKey));
        }

        /// <summary>
        /// Prepares the new object metadata properties.
        /// </summary>
        /// <typeparam name="T">Type of the item to create.</typeparam>
        /// <param name="item">The item to create.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="setRecFields">A flag indicating whether recXxx metadata properties should be set.</param>
        /// <returns>
        /// JObject representation of the item with metadata properties populated.
        /// </returns>
        protected JObject PrepareNewObjectProperties<T>(T item, string partitionKey, bool setRecFields = true)
        {
            // prepare new object
            var jObj = JObject.FromObject(item, JsonSerializer.Create(Util.SerializerSettings));
            PrepareNewObjectProperties(jObj, partitionKey, setRecFields);
            return jObj;
        }


        /// <summary>
        /// Prepares the new object metadata properties.
        /// </summary>
        /// <param name="jObj">The j object.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="setRecFields">A flag indicating whether recXxx metadata properties should be set.</param>
        protected void PrepareNewObjectProperties(JObject jObj, string partitionKey, bool setRecFields = true)
        {
            // prepare new object
            jObj["partition_key"] = partitionKey;
            jObj["id"] = Guid.NewGuid().ToString();

            if (setRecFields)
            {
                var userFullName = _appContextProvider.GetCurrentUserFullName();
                jObj["createdBy"] = userFullName;

                jObj["recStatus"] = "Active";
                jObj["recEffDt"] = DateTime.UtcNow;
                jObj["recExpDt"] = (string)null;
                jObj["recSource"] = "LEAP Backend API";
                // jObj["recSourceFile"] = (string)null;
            }
        }

        /// <summary>
        /// Creates the transactional batch.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <returns>the transactional batch</returns>
        protected TransactionalBatch CreateTransactionalBatch(string containerName, string partitionKey)
        {
            var container = GetContainer(containerName);
            return container.CreateTransactionalBatch(new PartitionKey(partitionKey));
        }

        /// <summary>
        /// Executes the transactional batch, and handles error responses.
        /// </summary>
        /// <param name="batch">The transactional batch.</param>
        protected static async Task ExecuteBatchAsync(TransactionalBatch batch)
        {
            if (batch == null)
            {
                return;
            }

            var result = await batch.ExecuteAsync();
            if (result.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new DataConflictException("Resource was updated by someone else, please reload resource and try again.");
            }
            else if (!result.IsSuccessStatusCode)
            {
                throw new ServiceException($"Couldn't update resource. {result.ErrorMessage}");
            }
        }

        #endregion

        #region Common DB queries

        /// <summary>
        /// Loads the Campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching Campus</returns>
        protected async Task<JObject> LoadCampusAsync(string campusId, string sql)
        {
            return await LoadLocationAsync(NodeType.Campus, campusId, sql);
        }

        /// <summary>
        /// Loads the region.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching region</returns>
        protected async Task<JObject> LoadRegionAsync(string regionId, string sql)
        {
            return await LoadLocationAsync(NodeType.Region, regionId, sql);
        }

        /// <summary>
        /// Loads the Child Location.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching child location</returns>
        protected async Task<JObject> LoadChildLocationAsync(string node, string sql = null)
        {
            sql ??= $"select * from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{node}' {ActiveRecordFilter}";
            return await LoadLocationAsync(NodeType.ChildLoc, node, sql);
        }

        /// <summary>
        /// Loads the Child Location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The child location Id.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>
        /// The matching child location
        /// </returns>
        /// <exception cref="EntityNotFoundException">Child Location with Node '{node}' was not found.</exception>
        protected async Task<JObject> LoadLocationAsync(NodeType nodeType, string node, string sql)
        {
            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            var jObj = queryResult.FirstOrDefault();
            if (jObj == null)
            {
                throw new EntityNotFoundException($"{nodeType} with Node '{node}' was not found.");
            }

            return jObj;
        }

        /// <summary>
        /// Loads the associates.
        /// </summary>
        /// <param name="associateIds">The associate ids.</param>
        /// <param name="limit">Limit the response</param>
        /// <returns>The associates.</returns>
        protected async Task<IList<AssociateModel>> LoadAssociatesAsync(IList<string> associateIds, bool limit = false)
        {
            string[] propsToRetrieve =
                {
                    "c.associateId",
                    "c.associateFirstName as firstName",
                    "c.associateMiddleName as middleName",
                    "c.associateLastName as lastName",
                    "c.associateTitle as title",
                    "c.associateEmail as Email"
                };

            var propList = string.Join(", ", propsToRetrieve);
            var associateIdsCsv = string.Join("','", associateIds);

            // load Associates
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.AssociatePartitionKey}' and c.associateId IN('{associateIdsCsv}') and IS_NULL(c.associateEndDate)=true {ActiveRecordFilter} ORDER BY c.associateFirstName ASC";
            if (limit)
            {
                sql += $" offset 0 limit 5";
            }
            var associates = await GetAllItemsAsync<AssociateModel>(CosmosConfig.SecondaryContainerName, sql);
            return associates;
        }

        /// <summary>
        /// Loads the contacts.
        /// </summary>
        /// <param name="associateIds">The associate ids.</param>
        /// <returns>the contacts</returns>
        protected async Task<IList<TitledContact>> LoadContactsAsync(IList<string> associateIds)
        {
            var result = new List<TitledContact>();
            if (associateIds?.Count > 0)
            {
                var associates = await LoadAssociatesAsync(associateIds, true);
                foreach (var associate in associates)
                {
                    var values = new string[] { associate.FirstName, associate.MiddleName, associate.LastName };
                    var nonEmptyValues = values.Where(x => !string.IsNullOrWhiteSpace(x));
                    var name = string.Join(" ", nonEmptyValues);
                    result.Add(new TitledContact
                    {
                        AssociateId = associate.AssociateId,
                        Name = name,
                        Title = associate.Title,
                        Email = associate.Email
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the campus node Id by region node Id.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <returns>The campus node Id.</returns>
        protected async Task<string> GetCampusIdByRegionIdAsync(string regionNodeId)
        {
            var sql = $"select value c.campusNodeId from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionNodeId}' {ActiveRecordFilter}";
            var campusNodeId = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return campusNodeId;
        }

        protected async Task<RegionDetailsModel> LoadRegionDetailsAsync(string regionNode, string[] propsToRetrieve)
        {
            var regionObj = await LoadRegionObjAsync(regionNode, propsToRetrieve);
            var result = regionObj.ToObject<RegionDetailsModel>();
            return result;
        }

        protected async Task<JObject> LoadRegionObjAsync(string regionNode, string[] propsToRetrieve)
        {
            // load Region
            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionNode}' {ActiveRecordFilter}";
            JObject regionObj = await LoadRegionAsync(regionNode, sql);
            return regionObj;
        }

        /// <summary>
        /// Gets the Region node Id by Child Location node Id.
        /// </summary>
        /// <param name="node">Child Location node Id.</param>
        /// <returns>The Region node Id.</returns>
        protected async Task<string> GetRegionIdByChildLocationNode(string node)
        {
            var sql = $"select value c.regionNodeId from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{node}' {ActiveRecordFilter}";
            var regionNodeId = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return regionNodeId;
        }

        /// <summary>
        /// Gets the child region Ids.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <returns>
        /// The child region Ids.
        /// </returns>
        protected async Task<IList<string>> GetRegionIdsAsync(string campusId)
        {
            // load Ids of Campus Regions
            string sql = $"SELECT VALUE c.node FROM c WHERE c.partition_key='{CosmosConfig.RegionPartitionKey}' AND c.campusNodeId='{campusId}' {ActiveRecordFilter}";
            var regionIds = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return regionIds;
        }

        /// <summary>
        /// Loads the child location Ids.
        /// </summary>
        /// <param name="regionIds">The region ids.</param>
        /// <returns>
        /// The child locations
        /// </returns>
        protected async Task<IList<string>> GetChildLocationIdsAsync(IList<string> regionIds)
        {
            var regionIdsCsv = string.Join("','", regionIds);

            // load Child Locations Ids
            string sql = $"SELECT VALUE c.node FROM c WHERE c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' AND c.regionNodeId in('{regionIdsCsv}') {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        ///// <summary>
        ///// Gets the campus node lookup by region node Id.
        ///// </summary>
        ///// <param name="regionNodeId">The region node identifier.</param>
        ///// <returns>The campus node Id.</returns>
        //protected async Task<string> GetCampusLookupByRegionIdAsync(string regionNodeId)
        //{
        //    var sql = $"select value {{ c.locationName as name, c.campusNodeId from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{regionNodeId}' {ActiveRecordFilter}";
        //    var campusNodeId = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);
        //    return campusNodeId;
        //}

        ///// <summary>
        ///// Gets the Region node Id by Child Location node Id.
        ///// </summary>
        ///// <param name="node">Child Location node Id.</param>
        ///// <returns>The Region node Id.</returns>
        //protected async Task<string> GetRegionIdByChildLocationNode(string node)
        //{
        //    var sql = $"select value c.regionNodeId from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{node}' {ActiveRecordFilter}";
        //    var regionNodeId = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);
        //    return regionNodeId;
        //}

        /// <summary>
        /// Gets the financial data item.
        /// </summary>
        /// <param name="locationId">The location ID.</param>
        /// <param name="regionId">The region Id.</param>
        /// <param name="address">The address.</param>
        /// <param name="costCenterId">The cost center ID.</param>
        /// <param name="lobCc">The LOB CC.</param>
        /// <returns></returns>
        protected async Task<FinancialDataItem> FindEdmcsFinancialDataAsync(string locationId, string regionId, NodeAddress address, string costCenterId, string lobCc, bool primaryPropsOnly = false)
        {
            // search matched items
            var matchedItems = await SearchEdmcsFinancialDataItemsAsync(locationId, regionId, address, costCenterId, lobCc, primaryPropsOnly);

            // get first item only
            var result = matchedItems.FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Gets the financial data item.
        /// </summary>
        /// <param name="locationId">The location ID.</param>
        /// <param name="regionId">The region Id.</param>
        /// <param name="address">The address.</param>
        /// <param name="costCenterId">The optional cost center ID criteria.</param>
        /// <param name="lobCc">The optional LOB CC criteria.</param>
        /// <returns></returns>
        protected async Task<IList<FinancialDataItem>> SearchEdmcsFinancialDataItemsAsync(string locationId, string regionId, NodePrimaryAddress address, string costCenterId = null, string lobCc = null, bool primaryPropsOnly = false)
        {
            var propsToRetrieve = _financialDataPrimaryProps;
            if (!primaryPropsOnly)
            {
                // NOTE: do not use AddRange, because it will alter the original list of values
                propsToRetrieve = propsToRetrieve.Concat(_financialDataSecondaryProps).ToList();
            }

            string propList = string.Join(", ", propsToRetrieve.Select(x => $"c.{x}"));
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
                .AppendEqualsCondition("address.addressLine1", address.AddressLine1)
                .AppendEqualsCondition("costCenterID", costCenterId)
                .AppendEqualsCondition("lobCc", lobCc);

            string sql = $"select {propList} from c {whereQuery}";

            // get result
            var result = await GetAllItemsAsync<FinancialDataItem>(CosmosConfig.LocationsContainerName, sql);

            // set Pricing Region values
            await SetPricingRegionValues(result);

            return result;
        }

        private async Task SetPricingRegionValues(IList<FinancialDataItem> items)
        {
            // retrieve Pricing Region values from mapping
            string[] propsToRetrieve =
                {
                    "c.trilogieLogon",
                    "c.pricingRegion"
                };

            // prepare query
            var propList = string.Join(", ", propsToRetrieve);
            string trilogieLogonsCsv = items.Select(x => x.TrilogieLogon).ToHashSet().StringJoin("','");
            string whereExpr = $"where c.trilogieLogon IN('{trilogieLogonsCsv}') AND c.partition_key='{CosmosConfig.PricingRegionMappingPartitionKey}' {ActiveRecordFilter}";
            string sql = $"SELECT {propList} FROM c {whereExpr}";

            var mappings = await GetAllItemsAsync<PricingRegionMappingModel>(CosmosConfig.ApplicationContainerName, sql);

            // set Pricing Region
            foreach (var item in items)
            {
                var mapping = mappings.FirstOrDefault(x => x.TrilogieLogon == item.TrilogieLogon);
                if (mapping != null)
                {
                    item.PricingRegion = mapping.PricingRegion;
                }
            }
        }

        /// <summary>
        /// Checks the Region exists.
        /// </summary>
        /// <param name="regionNodeId">The Region Node Id.</param>
        protected async Task CheckRegionExistsAsync(string regionNodeId)
        {
            string sql = $"select value Count(1) from c where c.partition_key='{CosmosConfig.RegionPartitionKey}'" +
                $" and c.node='{regionNodeId}' {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, sql);
            if (count == 0)
            {
                throw new EntityNotFoundException($"Region with Id '{regionNodeId}' was not found.");
            }
        }

        /// <summary>
        /// Gets the paging statement.
        /// </summary>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        protected static string GetPagingStatement(int pageNum, int pageSize)
        {
            if (pageSize < 1)
            {
                return string.Empty;
            }

            int offset = (pageNum - 1) * pageSize;
            return $"offset {offset} limit {pageSize}";
        }

        protected string GetAllLocationsPartitionKeyFilter()
        {
            var result =
                "(" +
                $"c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'" +
                $" or c.partition_key='{CosmosConfig.CampusPartitionKey}'" +
                $" or c.partition_key='{CosmosConfig.RegionPartitionKey}'" +
                ")";
            return result;
        }

        #endregion
    }
}
