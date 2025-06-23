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
    /// The Hierarchy service.
    /// </summary>
    public class HierarchyService : BaseCosmosService, IHierarchyService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchyService"/> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        public HierarchyService(CosmosClient cosmosClient, IOptions<CosmosConfig> cosmosConfig)
            : base(cosmosClient, cosmosConfig)
        {
        }

        /// <summary>
        /// Retrieves the Base hierarchy structure.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="regionId">The region identifier.</param>
        /// <returns>
        /// The Base hierarchy structure
        /// </returns>
        public async Task<IList<HierarchyNode>> GetBaseStructureAsync(string campusId, string regionId)
        {
            string partitionKey = null;

            // load Campuses
            string filterSql = "";
            if (!string.IsNullOrWhiteSpace(regionId))
            {
                // region filter, get CHild Locations
                partitionKey = CosmosConfig.ChildLocationPartitionKey;
                filterSql += $" and c.regionNodeId='{regionId}'";
            }
            else if (!string.IsNullOrWhiteSpace(campusId))
            {
                // campus filter, get Regions
                partitionKey = CosmosConfig.RegionPartitionKey;
                filterSql += $" and c.campusNodeId='{campusId}'";
            }
            else
            {
                // no filter, get Campuses
                partitionKey = CosmosConfig.CampusPartitionKey;
            }

            string sql = $"select {HierarchyNodeFieldSet} from c where c.partition_key='{partitionKey}' {filterSql} {ActiveRecordFilter}";

            var allItems = await GetAllItemsAsync<HierarchyNode>(CosmosConfig.LocationsContainerName, sql, partitionKey);
            allItems = allItems.OrderBy(x => x.Name).ToList();
            return allItems;
        }

        /// <summary>
        /// Gets the parents info for the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        public async Task<HierarchyNodeParentsInfo> GetBaseStructureParentsInfoAsync(HierarchyNodeType nodeType, string nodeId)
        {
            var result = new HierarchyNodeParentsInfo();

            if (nodeType == HierarchyNodeType.childLoc)
            {
                result.RegionId = await GetRegionIdByChildLocationNode(nodeId);
            }

            var regionId = result.RegionId ?? nodeId;
            result.CampusId = await GetCampusIdByRegionIdAsync(regionId);

            return result;
        }

        /// <summary>
        /// Gets the Base Hierarchy node details.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The Base Hierarchy node details.
        /// </returns>
        public async Task<BaseStructureNodeInfo> GetBaseStructureNodeInfoAsync(HierarchyNodeType nodeType, string nodeId)
        {
            var result = new BaseStructureNodeInfo();
            string sql;

            // load child location data, if needed
            if (nodeType == HierarchyNodeType.childLoc)
            {
                result.ChildLocId = nodeId;

                sql = $"select c.locationName, c.regionNodeId from c where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and c.node='{nodeId}' {ActiveRecordFilter}";
                var jObj = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
                result.ChildLocName = jObj.Value<string>("locationName");
                result.RegionId = jObj.Value<string>("regionNodeId");
            }

            if (nodeType == HierarchyNodeType.region)
            {
                result.RegionId = nodeId;
            }

            // load region data, if needed
            if (result.RegionId != null)
            {
                sql = $"select c.locationName, c.campusNodeId from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node='{result.RegionId}' {ActiveRecordFilter}";
                var jObj = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
                result.RegionName = jObj.Value<string>("locationName");
                result.CampusId = jObj.Value<string>("campusNodeId");
            }

            if (nodeType == HierarchyNodeType.campus)
            {
                result.CampusId = nodeId;
            }

            // load campus name
            sql = $"select value c.locationName from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node='{result.CampusId}' {ActiveRecordFilter}";
            result.CampusName = await GetValueAsync<string>(CosmosConfig.LocationsContainerName, sql);

            return result;
        }


        /// <summary>
        /// Retrieves the Physical hierarchy structure.
        /// </summary>
        /// <returns>The Physical hierarchy structure</returns>
        public async Task<IList<HierarchyNode>> GetPhysicalStructureAsync()
        {
            // Region -> State -> City - > Child Location

            // construct query
            string sql = $"select {HierarchyNodeFieldSet}, c.address, c.regionNodeId, c.regionID, c.locationName from c" +
                $" where c.partition_key='{CosmosConfig.RegionPartitionKey}' and" +
                $" IS_NULL(c.address.state)=false and IS_NULL(c.address.cityName)=false {ActiveRecordFilter}";

            var result = new List<HierarchyNode>();

            // get all regions
            var regions = await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql);
            // sort regions by name
            regions = regions.OrderBy(x => x.LocationName.ToLower()).ToList();
            foreach (var item in regions)
            {
                // Region
                var region = result.FirstOrDefault(x => x.Id == item.RegionId);
                if (region == null)
                {
                    region = new HierarchyNode
                    {
                        Id = item.RegionId,
                        Name = item.LocationName,
                        NodeType = HierarchyNodeType.region,
                        Children = new List<HierarchyNode>()
                    };
                    result.Add(region);
                }

                // State
                var state = region.Children.FirstOrDefault(x => x.Name == item.Address.State);
                if (state == null)
                {
                    state = new HierarchyNode
                    {
                        Id = $"{region.Id}-{item.Address.State}",
                        Name = item.Address.State,
                        NodeType = HierarchyNodeType.state,
                        Children = new List<HierarchyNode>()
                    };
                    region.Children.Add(state);
                }

                // City
                var city = state.Children.FirstOrDefault(x => x.Name == item.Address.CityName);
                if (city == null)
                {
                    city = new HierarchyNode
                    {
                        Id = $"{region.Id}-{item.Address.State}-{item.Address.CityName}",
                        Name = item.Address.CityName,
                        NodeType = HierarchyNodeType.city
                    };
                    state.Children.Add(city);
                }
            }

            result = Util.OrderStructureByName(result);
            return result;
        }

        /// <summary>
        /// Gets the Physical child locations based on given parameters.
        /// </summary>
        /// <param name="regionId">The region identifier.</param>
        /// <param name="state">The state.</param>
        /// <param name="cityName">Name of the city.</param>
        /// <returns>
        /// The Physical child locations.
        /// </returns>
        public async Task<IList<HierarchyNode>> GetPhysicalLocationsAsync(string regionId, string state, string cityName)
        {
            // construct query
            string sql = $"select {HierarchyNodeFieldSet}, c.regionNodeId, c.regionID, c.locationName from c" +
                $" where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}' and" +
                $" IS_NULL(c.address.state)=false and IS_NULL(c.address.cityName)=false {ActiveRecordFilter} and" +
                $" c.address.state='{state}' and c.address.cityName='{cityName}' and" +
                $" c.regionID='{regionId}'";

            var result = new List<HierarchyNode>();

            var childLocations = await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql);
            foreach (var item in childLocations)
            {
                var locationType = result.FirstOrDefault(x => x.Id == item.Id);
                if (locationType == null)
                {
                    locationType = new HierarchyNode
                    {
                        Id = item.Id,
                        Name = item.LocationName,
                        LocationType = item.LocationType,
                        NodeType = HierarchyNodeType.childLoc
                    };
                    result.Add(locationType);
                }
            }

            return result.OrderBy(x => x.Name.ToLower()).ToList();
        }

        /// <summary>
        /// Gets the parents info for the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        public async Task<HierarchyNodeParentsInfo> GetPhysicalStructureParentsInfoAsync(string nodeId)
        {
            // construct query
            string sql = $"select c.address.state, c.address.cityName, c.regionID from c" +
                $" where c.partition_key='{CosmosConfig.ChildLocationPartitionKey}'" +
                $" and c.node='{nodeId}' {ActiveRecordFilter}";

            var result = await GetFirstOrDefaultAsync<HierarchyNodeParentsInfo>(CosmosConfig.LocationsContainerName, sql);
            return result;
        }


        /// <summary>
        /// Retrieves the management hierarchy structure.
        /// </summary>
        /// <returns>The management hierarchy structure</returns>
        public async Task<IList<HierarchyNode>> GetManagementStructureAsync()
        {
            // LOB -> Region -> District -> Area -> LOB CC -> Inventory Org

            var allItems = new List<HierarchyNode>();

            var sql = $"select c.address, c.locationName, c.locationType, c.regionNodeId, c.inventoryOrg, c.districtID, c.districtName," +
                $" c.areaID, c.areaName, c.lobId, c.lobCc, c.lobCcName, c.lobDescription, c.regionID, c.regionName" +
                $" from c where c.partition_key='{CosmosConfig.EdmcsMasterPartitionKey}' {ActiveRecordFilter}";

            var data = await GetAllItemsAsync<LocationDoc>(CosmosConfig.LocationsContainerName, sql);
            foreach (var item in data)
            {
                // LOB
                var lob = allItems.FirstOrDefault(x => x.Id == item.LobId);
                if (lob == null)
                {
                    lob = new HierarchyNode
                    {
                        Id = item.LobId,
                        Name = item.LobDescription,
                        NodeType = HierarchyNodeType.lob,
                        Children = new List<HierarchyNode>()
                    };
                    allItems.Add(lob);
                }

                // Region
                var region = lob.Children.FirstOrDefault(x => x.Id == item.RegionId);
                if (region == null)
                {
                    region = new HierarchyNode
                    {
                        Id = item.RegionId,
                        Name = item.RegionName,
                        LocationId = item.RegionNodeId,
                        NodeType = HierarchyNodeType.region,
                        Children = new List<HierarchyNode>()
                    };
                    lob.Children.Add(region);
                }

                // District
                var district = region.Children.FirstOrDefault(x => x.Id == item.DistrictId);
                if (district == null)
                {
                    district = new HierarchyNode
                    {
                        Id = item.DistrictId,
                        Name = item.DistrictName,
                        NodeType = HierarchyNodeType.district,
                        Children = new List<HierarchyNode>()
                    };
                    region.Children.Add(district);
                }

                // Area
                var area = district.Children.FirstOrDefault(x => x.Id == item.AreaId);
                if (area == null)
                {
                    area = new HierarchyNode
                    {
                        Id = item.AreaId,
                        Name = item.AreaName,
                        NodeType = HierarchyNodeType.area,
                        Children = new List<HierarchyNode>()
                    };
                    district.Children.Add(area);
                }

                // Lob CC
                var lobCc = area.Children.FirstOrDefault(x => x.Id == item.LobCc);
                if (lobCc == null)
                {
                    lobCc = new HierarchyNode
                    {
                        Id = item.LobCc,
                        Name = item.LobCcName,
                        NodeType = HierarchyNodeType.lobCc,
                        Children = new List<HierarchyNode>()
                    };
                    area.Children.Add(lobCc);
                }

                // Inventory Org
                var inventoryOrg = lobCc.Children.FirstOrDefault(x => x.Id == $"{lobCc.Id}-{item.InventoryOrg}");
                if (inventoryOrg == null && !string.IsNullOrWhiteSpace(item.InventoryOrg) && item.InventoryOrg != "NA" &&
                    item.InventoryOrg != "TBD")
                {
                    inventoryOrg = new HierarchyNode
                    {
                        Id = $"{lobCc.Id}-{item.InventoryOrg}",
                        Name = item.InventoryOrg,
                        NodeType = HierarchyNodeType.inventoryOrg
                    };
                    lobCc.Children.Add(inventoryOrg);
                }
            }

            allItems = Util.OrderStructureByName(allItems);
            return allItems;
        }

        /// <summary>
        /// Loads the region.
        /// </summary>
        /// <param name="regionNodeId">The region node id.</param>
        /// <returns>
        /// The region
        /// </returns>
        private async Task<JObject> LoadRegionAsync(string regionNodeId)
        {
            if (regionNodeId == null)
            {
                return null;
            }

            // load Child Locations
            var sql = $"select * from c where c.partition_key='{CosmosConfig.RegionPartitionKey}' and c.node = '{regionNodeId}' {ActiveRecordFilter}";
            var results = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Loads the campus.
        /// </summary>
        /// <param name="campusNodeId">The campus node id.</param>
        /// <returns>
        /// The campus
        /// </returns>
        private async Task<JObject> LoadCampusAsync(string campusNodeId)
        {
            if (campusNodeId == null)
            {
                return null;
            }

            // load Child Locations
            var sql = $"select * from c where c.partition_key='{CosmosConfig.CampusPartitionKey}' and c.node = '{campusNodeId}' {ActiveRecordFilter}";
            var results = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return results.FirstOrDefault();
        }
    }
}
