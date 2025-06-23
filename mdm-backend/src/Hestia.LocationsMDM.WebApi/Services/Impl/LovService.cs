using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The List of Values service.
    /// </summary>
    public class LovService : BaseCosmosService, ILovService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LovService"/> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public LovService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, appContextProvider)
        {
        }

        #region CRUDS

        /// <summary>
        /// Searches the list of values matching given criteria.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sortBy">The sort by.</param>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>
        /// The macthed calendar events.
        /// </returns>
        public async Task<SearchResult<LovItemModel>> SearchAsync(
            string key,
            string sortBy,
            int pageNum,
            int pageSize)
        {
            string[] propsToRetrieve =
                {
                    "c.valueId",
                    "c.sequence",
                    "c.key",
                    "c['value']"
                };

            string propList = string.Join(", ", propsToRetrieve);

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.LovPartitionKey}' {ActiveRecordFilter}");
            whereQuery
                .AppendEqualsCondition("key", key);

            // specify Order By
            sortBy = string.IsNullOrWhiteSpace(sortBy) ? "sequence" : sortBy;
            string orderBy = $"ORDER BY c.{sortBy}";

            var result = new SearchResult<LovItemModel>();

            // construct query to get page results
            int offset = (pageNum - 1) * pageSize;
            string sql = $"select {propList} from c {whereQuery} {orderBy} offset {offset} limit {pageSize}";

            // get page items
            result.Items = await GetAllItemsAsync<LovItemModel>(CosmosConfig.ApplicationContainerName, sql);

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.ApplicationContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets the list of values for the given Key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// The list of values.
        /// </returns>
        public async Task<IList<T>> GetAllValuesAsync<T>(string key)
        {
            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.LovPartitionKey}' {ActiveRecordFilter}");
            whereQuery
                .AppendEqualsCondition("key", key);

            // construct query to get results
            string sql = $"select value c.values from c {whereQuery}";

            // get result items
            var result = await GetAllItemsAsync<List<T>>(CosmosConfig.ApplicationContainerName, sql);

            // must be exactly one item (according to new approach)
            if (result.Count == 0)
            {
                throw new EntityNotFoundException($"LOV values with Key={key} was not found.");
            }
            if (result.Count > 1)
            {
                throw new ApplicationException($"There are multiple LOV records with Key={key}. There must be only 1 Active record in DB.");
            }

            // apply default ascending order
            if (result[0].Count > 1 && result[0][0] is IComparable)
            {
                result[0].Sort();
            }
            return result[0];
        }

        /// <summary>
        /// Gets the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <returns>
        /// The List of Value item details.
        /// </returns>
        public async Task<LovItemModel> GetAsync(string valueId)
        {
            string[] propsToRetrieve =
                {
                    "c.valueId",
                    "c.sequence",
                    "c.key",
                    "c['value']"
                };

            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.LovPartitionKey}' and c.valueId='{valueId}' {ActiveRecordFilter}";
            var jObj = await LoadLovItemAsync(valueId, sql);

            var model = jObj.ToObject<LovItemModel>();
            return model;
        }

        /// <summary>
        /// Creates the List of Value item.
        /// </summary>
        /// <param name="model">The List of Value item data.</param>
        /// <returns>Created List of Value item model.</returns>
        public async Task<LovItemModel> CreateAsync(LovItemModel model)
        {
            await CheckDuplicateDoesNotExist(model.Key, model.Value);

            // create LOV item
            model.ValueId = Guid.NewGuid().ToString();
            await CreateItemAsync(CosmosConfig.ApplicationContainerName, CosmosConfig.LovPartitionKey, model);
            return model;
        }

        /// <summary>
        /// Updates the List of Value item.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <param name="model">The updated List of Value item data.</param>
        public async Task UpdateAsync(string valueId, LovItemPatchModel model)
        {
            // load Event Model
            string sql = $"select * from c where c.partition_key='{CosmosConfig.LovPartitionKey}' and c.valueId='{valueId}' {ActiveRecordFilter}";
            var jObj = await LoadLovItemAsync(valueId, sql);

            await CheckDuplicateDoesNotExist(
                model.Key ?? jObj.Value<string>("key"),
                model.Value ?? jObj.Value<string>("value"),
                valueId);

            // perform clone using audit trail
            JToken newObj = PrepareAndCloneObject(jObj);

            // set updated values
            newObj.UpdateOptionalProperty("key", model.Key);
            newObj.UpdateOptionalProperty("value", model.Value);
            newObj.UpdateOptionalProperty("sequence", model.Sequence);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.ApplicationContainerName, CosmosConfig.LovPartitionKey, jObj, newObj);
        }

        /// <summary>
        /// Deletes the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        public async Task DeleteAsync(string valueId)
        {
            // load Calendar Event
            string sql = $"select * from c where c.partition_key='{CosmosConfig.LovPartitionKey}' and c.valueId='{valueId}' {ActiveRecordFilter}";
            var jObj = await LoadLovItemAsync(valueId, sql);

            // perform clone using audit trail
            JToken newObj = PrepareDeleteAndCloneObject(jObj);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.ApplicationContainerName, CosmosConfig.LovPartitionKey, jObj, newObj);
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Loads the List of Value item.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching List of Value item.</returns>
        private async Task<JObject> LoadLovItemAsync(string valueId, string sql)
        {
            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.ApplicationContainerName, sql);
            var jObj = queryResult.FirstOrDefault();
            if (jObj == null)
            {
                throw new EntityNotFoundException($"List of Value item with Id '{valueId}' was not found.");
            }

            return jObj;
        }

        /// <summary>
        /// Checks the duplicate LOV item doesn't exist.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="currentItemId">The current item Id, in case item is being updated.</param>
        private async Task CheckDuplicateDoesNotExist(string key, string value, string currentItemId = null)
        {
            string extraFilter = string.Empty;
            if (currentItemId != null)
            {
                // exclude current item
                extraFilter = $"AND c.valueId != '{currentItemId}'";
            }

            // make sure there is no LOV item with the same key/value
            string sql = $"SELECT VALUE Count(1) FROM c WHERE c.partition_key='{CosmosConfig.LovPartitionKey}'" +
                $" AND LOWER(c.key)='{key.ToLower()}' AND LOWER(c['value'])='{value.ToLower()}' {extraFilter} {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.ApplicationContainerName, sql);
            if (count > 0)
            {
                throw new DataConflictException($"List of Value item with key/value '{key}'/'{value}' already exists.");
            }
        }

        #endregion
    }
}
