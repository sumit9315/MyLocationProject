using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Dashboard service.
    /// </summary>
    public class DashboardService : BaseCosmosService, IDashboardService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService"/> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        public DashboardService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig)
            : base(cosmosClient, cosmosConfig)
        {
        }

        /// <summary>
        /// Gets the Dashboard statistics.
        /// </summary>
        /// <returns>The Dashboard statistics.</returns>
        public async Task<DashboardStatisticsModel> GetStatisticsAsync()
        {
            const string nonEmptyAddressCondition = "and LENGTH(c.address.addressLine1) > 0";
            string knownAddressCondition = $"{nonEmptyAddressCondition} and c.address.addressLine1 != 'TBD' and c.address.addressLine1 != 'NA'";

            var result = new DashboardStatisticsModel
            {
                ActiveCampus = await GetUniqueCountAsync(CosmosConfig.CampusPartitionKey, "node"),
                Areas = await GetUniqueCountAsync(CosmosConfig.EdmcsMasterPartitionKey, "areaID", knownAddressCondition),
                Districts = await GetUniqueCountAsync(CosmosConfig.EdmcsMasterPartitionKey, "districtID", knownAddressCondition),
                Regions = await GetUniqueCountAsync(CosmosConfig.RegionPartitionKey, "regionID")
            };

            return result;
        }

        /// <summary>
        /// Gets the unique count of field values in the given partition.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="whereCondition">The where condition.</param>
        /// <returns>
        /// The unique count of field values
        /// </returns>
        private async Task<int> GetUniqueCountAsync(string partitionKey, string fieldName, string whereCondition = "")
        {
            string sql = "SELECT COUNT(UniqueIDValues) AS UniqueCount" +
                $" FROM(SELECT c.{fieldName} FROM c where c.partition_key='{partitionKey}' {whereCondition} {ActiveRecordFilter} GROUP BY c.{fieldName}) AS UniqueIDValues";

            var result = await GetAllItemsAsync<UniqueCountModel>(CosmosConfig.LocationsContainerName, sql);
            var model = result[0];
            return model.UniqueCount;
        }

        ///// <summary>
        ///// Gets the unique count of field values in the given partition in Master EDMCS data.
        ///// </summary>
        ///// <param name="partitionKey">The partition key.</param>
        ///// <param name="fieldName">Name of the field.</param>
        ///// <param name="whereCondition">The where condition.</param>
        ///// <returns>
        ///// The unique count of field values
        ///// </returns>
        //private async Task<int> GetMasterDataUniqueCountAsync(string partitionKey, string fieldName, string whereCondition = null)
        //{
        //    string sql = "SELECT COUNT(UniqueIDValues) AS UniqueCount" +
        //        $" FROM(SELECT c.{fieldName} FROM c where c.partition_key='{partitionKey}' {ActiveRecordFilter} GROUP BY c.{fieldName}) AS UniqueIDValues";

        //    var result = await GetAllItemsAsync<UniqueCountModel>(CosmosConfig.LocationsContainerName, sql);
        //    var model = result[0];
        //    return model.UniqueCount;
        //}

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
    }
}
