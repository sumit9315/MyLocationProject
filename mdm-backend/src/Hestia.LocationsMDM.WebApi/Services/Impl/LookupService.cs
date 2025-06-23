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
    /// The Lookup service.
    /// </summary>
    public class LookupService : BaseCosmosService, ILookupService
    {
        private readonly ILovService _lovService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupService"/> class.
        /// </summary>
        /// <param name="lovService">The LOV service.</param>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        public LookupService(ILovService lovService, CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig)
            : base(cosmosClient, cosmosConfig)
        {
            _lovService = lovService;
        }

        #region Financial Data

        /// <summary>
        /// Finds the financial data by given parameters.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <param name="costCenterId">The cost center Id.</param>
        /// <param name="lobCc">The lob CC.</param>
        /// <returns>Found Financial Data, or null if not exists.</returns>
        public async Task<FinancialDataItem> FindFinancialDataAsync(string childLocNode, string costCenterId = null, string lobCc = null)
        {
            // load Child Location
            string sql = $"select c.locationID, c.regionID, c.address from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{childLocNode}' {ActiveRecordFilter}";
            var jObj = await LoadChildLocationAsync(childLocNode, sql);

            string locationId = jObj.Value<string>("locationID");
            string regionId = jObj.Value<string>("regionID");
            var address = jObj["address"].ToObject<NodeAddress>();

            FinancialDataItem financialData = await FindEdmcsFinancialDataAsync(locationId, regionId, address, costCenterId, lobCc, primaryPropsOnly: true);
            return financialData;
        }

        /// <summary>
        /// Gets the available financial data for the given child location node.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <returns>Available Financial Data lookups, or empty list if not exists.</returns>
        public async Task<IList<FinancialDataItem>> GetAvailableFinancialDatasAsync(string childLocNode)
        {
            // load Child Location
            string sql = $"select c.locationID, c.regionID, c.address from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{childLocNode}' {ActiveRecordFilter}";
            var jObj = await LoadChildLocationAsync(childLocNode, sql);

            string locationId = jObj.Value<string>("locationID");
            string regionId = jObj.Value<string>("regionID");
            var address = jObj["address"].ToObject<NodeAddress>();

            var result = await SearchEdmcsFinancialDataItemsAsync(locationId, regionId, address, primaryPropsOnly: true);
            return result;
        }

        #endregion

        /// <summary>
        /// Gets all available Location IDs for the given region.
        /// </summary>
        /// <returns>All available Location IDs.</returns>
        public async Task<IList<string>> GetRegionLocationIdsAsync(string regionNode)
        {
            string[] propsToRetrieve =
                {
                    "c.regionID as regionId",
                    "c.address"
                };

            var regionInfo = await LoadRegionDetailsAsync(regionNode, propsToRetrieve);
            var address = regionInfo.Address;
            
            // construct extra 'where' filter
            var whereQuery = new StringBuilder();
            string city = address.CityName.Replace("'","\\'");
            Console.WriteLine("City = " +city);
            whereQuery
                .AppendEqualsCondition("regionID", regionInfo.RegionId)
                .AppendEqualsCondition("address.state", address.State)
                .AppendEqualsCondition("address.cityName", city)
                .AppendEqualsCondition("address.postalCodePrimary", address.PostalCodePrimary)
                .AppendEqualsCondition("address.addressLine1", address.AddressLine1);

            string sql = BuildDistinctFieldQuery(CosmosConfig.EdmcsMasterPartitionKey, "locationID", extraFilter: whereQuery.ToString());
            var result = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets all unique region names.
        /// </summary>
        /// <returns>All unique region names.</returns>
        public async Task<IList<string>> GetRegionNamesAsync()
        {
            string sql = BuildDistinctFieldQuery(CosmosConfig.RegionPartitionKey, "regionName");
            var result = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets the distinct Trilogie Logon values from EDMCS.
        /// </summary>
        /// <returns>Distinct Trilogie Logon values.</returns>
        public async Task<IList<string>> GetDistinctTrilogieLogonsFromEdmcsAsync()
        {
            string sql = BuildDistinctFieldQuery(CosmosConfig.EdmcsMasterPartitionKey, "trilogieLogon");
            var result = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets all distinct Pricing Region values from Pricing Region mapping.
        /// </summary>
        /// <returns>Distinct Pricing Region values.</returns>
        public async Task<IList<string>> GetPricingRegionsValuesAsync()
        {
            string sql = BuildDistinctFieldQuery(CosmosConfig.PricingRegionMappingPartitionKey, "pricingRegion");
            var result = await GetAllItemsAsync<string>(CosmosConfig.ApplicationContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets all cities.
        /// </summary>
        /// <returns>All unique cities from locations.</returns>
        public async Task<IList<CityLookupModel>> GetCities()
        {
            string sql = "SELECT value { stateCode: groups.state, cityName: groups.cityName } FROM" +
                $"(SELECT c.address.state, c.address.cityName FROM c where c.partition_key='{CosmosConfig.EdmcsMasterPartitionKey}' GROUP BY c.address.state, c.address.cityName) AS groups order by groups.cityName";

            var result = await GetAllItemsAsync<CityLookupModel>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets the state lookups.
        /// </summary>
        /// <returns>
        /// The state lookups.
        /// </returns>
        public async Task<IList<LookupModel>> GetStatesAsync()
        {
            string sql = BuildDistinctFieldQuery(CosmosConfig.EdmcsMasterPartitionKey, "state", "address");
            var stateCodes = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);

            var result = stateCodes.Select(x => new LookupModel
            {
                Id = x
            }).ToList();

            return result;
        }

        /// <summary>
        /// Gets the countries with corresponding states.
        /// </summary>
        /// <returns>
        /// The countries with corresponding states.
        /// </returns>
        public async Task<IList<CountryStatesModel>> GetCountryStatesAsync()
        {
            // get all distinct Country names from DB
            string allLocsFilter = GetAllLocationsPartitionKeyFilter();
            string sql = $"SELECT distinct value c.address.countryName FROM c where c.partition_key='{CosmosConfig.CampusPartitionKey}' {ActiveRecordFilter}";
            var countryNames = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);

            // States for countries come from LOV data
            var countryStates = await _lovService.GetAllValuesAsync<CountryStatesModel>("countryStates");

            // merge Countries and Country States
            foreach (var countryName in countryNames)
            {
                if (!countryStates.Any(x => x.CountryName == countryName))
                {
                    countryStates.Add(new CountryStatesModel
                    {
                        CountryName = countryName
                    });
                }
            }

            // order by Country Name
            countryStates = countryStates.OrderBy(x => x.CountryName).ToList();
            return countryStates;
        }

        /// <summary>
        /// Gets all districts.
        /// </summary>
        /// <returns>All unique districts from locations.</returns>
        public async Task<IList<LookupModel>> GetDistricts()
        {
            string allLocsFilter = GetAllLocationsPartitionKeyFilter();
            string sql = "SELECT value { id: groups.districtID, name: groups.districtName } FROM" +
                $"(SELECT c.districtID, c.districtName FROM c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' {ActiveRecordFilter} GROUP BY c.districtID, c.districtName) AS groups order by groups.districtName";

            var result = await GetAllItemsAsync<LookupModel>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets all unique location types.
        /// </summary>
        /// <returns>All unique location types.</returns>
        public async Task<IList<string>> GetLocationTypesAsync()
        {
            string allLocsFilter = GetAllLocationsPartitionKeyFilter();
            string sql = BuildDistinctFieldsQuery(allLocsFilter, "locationType");
            var result = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        #region Campus & Region

        /// <summary>
        /// Gets the Campus lookups.
        /// </summary>
        /// <returns>
        /// The Campus lookups.
        /// </returns>
        public async Task<IList<LocationLookupModel>> GetAllCampusesAsync()
        {
            string[] propsToRetrieve =
                {
                    "c.node",
                    "c.locationName"
                };

            // load Campuses
            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<LocationLookupModel>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        /// <summary>
        /// Gets the region lookups of the Campus by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>
        /// The region lookups.
        /// </returns>
        public async Task<IList<LookupModel>> GetCampusRegionLookupsAsync(string campusId)
        {
            string[] propsToRetrieve =
                {
                    "c.node as id",
                    "c.locationName as name"
                };

            // load Region
            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.campusNodeId='{campusId}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<LookupModel>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }

        #endregion

        /// <summary>
        /// Gets the child location lookups of the Region by Id.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <returns>
        /// The child location lookups.
        /// </returns>
        public async Task<IList<ChildLocationLookupModel>> GetChildLocationLookupsAsync(string regionNodeId)
        {
            string[] propsToRetrieve =
                {
                    "c.node as id",
                    "c.locationName as name",
                    "c.locationType"
                };

            // load Region
            string propList = string.Join(", ", propsToRetrieve);
            string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.regionNodeId='{regionNodeId}' {ActiveRecordFilter}";
            var result = await GetAllItemsAsync<ChildLocationLookupModel>(CosmosConfig.LocationsContainerName, sql);
            return result.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Builds the distinct field query.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldPath">The field path.</param>
        /// <param name="extraFilter">The extra filter.</param>
        /// <returns>
        /// The constructed query.
        /// </returns>
        private static string BuildDistinctFieldQuery(string partitionKey, string fieldName, string fieldPath = null, string extraFilter = null)
        {
            string partitionKeysFiter = $"c.partition_key='{partitionKey}'";

            var result = BuildDistinctFieldsQuery($"({partitionKeysFiter})", fieldName, fieldPath, extraFilter);
            return result;
        }

        /// <summary>
        /// Builds the distinct field query.
        /// </summary>
        /// <param name="partitionKeys">The partition keys.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldPath">The field path.</param>
        /// <returns>
        /// The constructed query.
        /// </returns>
        private static string BuildDistinctFieldQuery(string[] partitionKeys, string fieldName, string fieldPath = null)
        {
            string partitionKeysFiter = partitionKeys
                .Select(x => $"c.partition_key='{x}'")
                .StringJoin(" or ");

            var result = BuildDistinctFieldsQuery($"({partitionKeysFiter})", fieldName, fieldPath);
            return result;
        }

        private static string BuildDistinctFieldsQuery(string partitionKeyFilter, string fieldName, string fieldPath = null, string extraFilter = null)
        {
            string fullFieldPath = fieldPath != null
                ? $"{fieldPath}.{fieldName}"
                : fieldName;
            string sql = $"SELECT value groups.{fieldName} FROM(SELECT c.{fullFieldPath} FROM c where {partitionKeyFilter} {extraFilter ?? ""} {ActiveRecordFilter} GROUP BY c.{fullFieldPath}) AS groups order by groups.{fieldName}";
            return sql;
        }
    }
}
