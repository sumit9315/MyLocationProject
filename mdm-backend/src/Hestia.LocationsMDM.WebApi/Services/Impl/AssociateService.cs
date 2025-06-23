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
    /// The Associate service.
    /// </summary>
    public class AssociateService : BaseCosmosService, IAssociateService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssociateService"/> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        public AssociateService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig, IAppContextProvider appContextProvider)
            : base(cosmosClient, cosmosConfig, appContextProvider)
        {
        }

        #region CRUDS

        /// <summary>
        /// Searches the associates matching given criteria.
        /// </summary>
        /// <param name="sortBy">The sort by criteria.</param>
        /// <param name="campusId">The campus Id criteria.</param>
        /// <param name="regionId">The region Id criteria.</param>
        /// <param name="childLocationId">The child location Id criteria.</param>
        /// <param name="associateId">The associate Id criteria.</param>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="title">The title.</param>
        /// <param name="isAssociated">The is associated flag.</param>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>
        /// The macthed associates.
        /// </returns>
        public async Task<SearchResult<AssociateSummaryModel>> SearchAsync(
            string sortBy,
            string campusId,
            string regionId,
            string childLocationId,
            string associateId,
            string firstName,
            string lastName,
            string title,
            bool isAssociated,
            int pageNum,
            int pageSize)
        {
            string[] propsToRetrieve =
                {
                    "c.associateId",
                    "c.associateFirstName as firstName",
                    "c.associateLastName as lastName",
                    "c.associateTitle as title"
                };

            string propList = string.Join(", ", propsToRetrieve);

            // check if need to filter by parent object
            IList<string> associateIdsFilter = null;
            bool objectProvided = true;
            if (!string.IsNullOrWhiteSpace(campusId))
            {
                associateIdsFilter = await GetObjectAssociatesAsync(CosmosConfig.CampusPartitionKey, "node", campusId.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(regionId))
            {
                associateIdsFilter = await GetObjectAssociatesAsync(CosmosConfig.RegionPartitionKey, "node", regionId.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(childLocationId))
            {
                associateIdsFilter = await GetObjectAssociatesAsync(CosmosConfig.ChildLocationPartitionKey, "node", childLocationId.Trim());
            }
            else
            {
                objectProvided = false;
            }

            var result = new SearchResult<AssociateSummaryModel>();

            if (isAssociated && objectProvided && (associateIdsFilter == null || associateIdsFilter.Count == 0))
            {
                result.Items = new List<AssociateSummaryModel>();
                result.TotalCount = 0;
                return result;
            }

            // construct 'where' filter
            var whereQuery = new StringBuilder($"where c.partition_key='{CosmosConfig.AssociatePartitionKey}' {ActiveRecordFilter} and IS_NULL(c.associateEndDate)=true");
            whereQuery
                .AppendContainsCondition("associateId", associateId)
                .AppendContainsCondition("associateFirstName", firstName)
                .AppendContainsCondition("associateLastName", lastName)
                .AppendContainsCondition("associateTitle", title);

            if (associateIdsFilter != null && associateIdsFilter.Count > 0)
            {
                if (isAssociated)
                {
                    whereQuery.AppendContainsCondition("associateId", associateIdsFilter);
                }
                else
                {
                    whereQuery.AppendNotContainsCondition("associateId", associateIdsFilter);
                }
            }

            // specify Order By
            string orderBy = string.Empty;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                string sortByProp = GetSortByPropName(sortBy.Split(':').First<string>());
                string direction = sortBy.Split(':').Length > 1 ? sortBy.Split(':').Last<string>() : "ASC";
                orderBy = $"ORDER BY c.{sortByProp} {direction.ToUpper()}";
            }

            // construct query to get page results
            string paging = GetPagingStatement(pageNum, pageSize);
            string sql = $"select {propList} from c {whereQuery} {orderBy} {paging}";

            // get page items
            result.Items = await GetAllItemsAsync<AssociateSummaryModel>(CosmosConfig.SecondaryContainerName, sql, CosmosConfig.AssociatePartitionKey);

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.SecondaryContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets the Associate by Id.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <returns>The Associate details.</returns>
        public async Task<AssociateDetailsModel> GetAsync(string associateId)
        {
            string[] propsToRetrieve =
                {
                    "c.associateFirstName as firstName",
                    "c.associateLastName as lastName",
                    "c.associateTitle as title",
                    "c.associatePhoneNumber as contactNumber",
                    "c.associateEmail as email"
                };

            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.AssociatePartitionKey}' and c.associateId='{associateId}' {ActiveRecordFilter}";
            var jObj = await LoadAssociateAsync(associateId, sql);

            var model = jObj.ToObject<AssociateDetailsModel>();
            model.AssociateId = associateId;

            return model;
        }

        /// <summary>
        /// Updates the Associate.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <param name="model">The updated Associate data.</param>
        public async Task UpdateAsync(string associateId, AssociatePatchModel model)
        {
            // load Child Location
            string sql = $"select * from c where c.partition_key='{CosmosConfig.AssociatePartitionKey}' and c.associateId='{associateId}' {ActiveRecordFilter}";
            var jObj = await LoadAssociateAsync(associateId, sql);

            // perform clone using audit trail
            JToken newObj = PrepareAndCloneObject(jObj);

            // set updated values
            newObj.UpdateOptionalProperty("associatePhoneNumber", model.ContactNumber);
            newObj.UpdateOptionalProperty("associateEmail", model.Email);

            // update item (using Audit Trail and Optimistic Concurrency)
            await UpdateItemAsync(CosmosConfig.SecondaryContainerName, CosmosConfig.AssociatePartitionKey, jObj, newObj);
        }

        #endregion


        #region helper methods

        /// <summary>
        /// Loads the Associate.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The matching Associate</returns>
        private async Task<JObject> LoadAssociateAsync(string associateId, string sql)
        {
            var queryResult = await GetAllItemsAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
            var jObj = queryResult.FirstOrDefault();
            if (jObj == null)
            {
                throw new EntityNotFoundException($"Associate with Id '{associateId}' was not found.");
            }

            return jObj;
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
                "firstName" => "associateFirstName",
                "lastName" => "associateLastName",
                "title" => "associateTitle",
                _ => sortBy,
            };
        }

        /// <summary>
        /// Loads the object associates.
        /// </summary>
        /// <param name="objectPartitionKey">The object partition key.</param>
        /// <param name="objectIdPropName">Name of the object identifier property.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <returns></returns>
        private async Task<IList<string>> GetObjectAssociatesAsync(string objectPartitionKey, string objectIdPropName, string objectId)
        {
            string sql = $"select value Count(1) from c where c.partition_key='{objectPartitionKey}'" +
                $" and c.{objectIdPropName}= '{objectId}' {ActiveRecordFilter}";

            var count = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, sql);
            if (count == 0)
            {
                throw new EntityNotFoundException($"Object with Id '{objectId}' was not found.");
            }

            // get 'associateId' from given object
            sql = "SELECT VALUE res.associateId" +
                " FROM (SELECT ARRAY(SELECT VALUE a.associateId FROM a IN c.associate) as associateId" +
                $" FROM c where c.partition_key='{objectPartitionKey}' and c.{objectIdPropName}='{objectId}' {ActiveRecordFilter}) as res";

            var associateIds = await GetFirstOrDefaultAsync<IList<string>>(CosmosConfig.LocationsContainerName, sql);
            return associateIds;
        }

        #endregion
    }
}
