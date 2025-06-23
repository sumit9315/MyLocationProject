using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using System.ComponentModel.DataAnnotations;
using Hestia.LocationsMDM.WebApi.Common;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Hierarchy controller.
    /// </summary>
    public class HierarchyController : BaseController
    {
        private readonly IHierarchyService _hierarchyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchyController"/> class.
        /// </summary>
        /// <param name="hierarchyService">The hierarchy service.</param>
        /// <param name="memoryCache">The hierarchy service.</param>
        public HierarchyController(IHierarchyService hierarchyService, IMemoryCache memoryCache)
            : base(memoryCache)
        {
            _hierarchyService = hierarchyService;
        }

        /// <summary>
        /// Gets the Base Hierarchy.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="regionId">The region identifier.</param>
        /// <returns>
        /// The Base Hierarchy.
        /// </returns>
        [HttpGet("base")]
        public async Task<IList<HierarchyNode>> GetBaseStructureAsync(string campusId = null, string regionId = null)
        {
            // NOTE: we are caching top level requests
            bool isTopLevel = campusId == null && regionId == null;

            IList<HierarchyNode> result;
            if (isTopLevel)
            {
                // try get data from cache
                if (!_memoryCache.TryGetValue(BaseHierarchyTopLevelCacheKey, out result))
                {
                    // data not in cache, so get from DB data
                    result = await _hierarchyService.GetBaseStructureAsync(campusId, regionId);

                    // Save data in cache and set the relative expiration time to one day
                    _memoryCache.Set(BaseHierarchyTopLevelCacheKey, result, TimeSpan.FromMinutes(60));
                }
            }
            else
            {
                result = await _hierarchyService.GetBaseStructureAsync(campusId, regionId);
            }

            return result;
        }

        /// <summary>
        /// Gets the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        [HttpGet("base/parentsInfo")]
        public async Task<HierarchyNodeParentsInfo> GetBaseStructureParentsInfoAsync(HierarchyNodeType nodeType, string nodeId)
        {
            var result = await _hierarchyService.GetBaseStructureParentsInfoAsync(nodeType, nodeId);
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
        [HttpGet("base/nodeInfo")]
        public async Task<BaseStructureNodeInfo> GetBaseStructureNodeInfoAsync(HierarchyNodeType nodeType, string nodeId)
        {
            var result = await _hierarchyService.GetBaseStructureNodeInfoAsync(nodeType, nodeId);
            return result;
        }


        /// <summary>
        /// Gets the Physical Hierarchy structure.
        /// </summary>
        /// <returns>
        /// The Physical Hierarchy structure.
        /// </returns>
        [HttpGet("physical/structure")]
        public async Task<IList<HierarchyNode>> GetPhysicalStructureAsync()
        {
            IList<HierarchyNode> result;

            // try get data from cache
            if (!_memoryCache.TryGetValue(PhysicalHierarchyTopLevelCacheKey, out result))
            {
                // data not in cache, so get data from DB
                result = await _hierarchyService.GetPhysicalStructureAsync();

                // Save data in cache and set the relative expiration time to one day
                _memoryCache.Set(PhysicalHierarchyTopLevelCacheKey, result, TimeSpan.FromMinutes(60));
            }

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
        [HttpGet("physical/childLocations")]
        public async Task<IList<HierarchyNode>> GetPhysicalLocationsAsync(string regionId, string state, string cityName)
        {
            Util.ValidateArgumentNotNullOrEmpty(regionId, nameof(regionId));
            Util.ValidateArgumentNotNullOrEmpty(state, nameof(state));
            Util.ValidateArgumentNotNullOrEmpty(cityName, nameof(cityName));

            var result = await _hierarchyService.GetPhysicalLocationsAsync(regionId, state, cityName);
            return result;
        }

        /// <summary>
        /// Gets the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        [HttpGet("physical/parentsInfo")]
        public async Task<HierarchyNodeParentsInfo> GetPhysicalStructureParentsInfoAsync(string nodeId)
        {
            var result = await _hierarchyService.GetPhysicalStructureParentsInfoAsync(nodeId);
            return result;
        }


        /// <summary>
        /// Retrieves the management hierarchy structure.
        /// </summary>
        /// <returns>The management hierarchy structure</returns>
        [HttpGet("management")]
        public async Task<IList<HierarchyNode>> GetManagementStructureAsync()
        {
            IList<HierarchyNode> result;

            // try get data from cache
            if (!_memoryCache.TryGetValue(ManagementHierarchyTopLevelCacheKey, out result))
            {
                // data not in cache, so get data from DB
                result = await _hierarchyService.GetManagementStructureAsync();

                // Save data in cache and set the relative expiration time to one day
                _memoryCache.Set(ManagementHierarchyTopLevelCacheKey, result, TimeSpan.FromMinutes(60));
            }

            return result;
        }
    }
}
