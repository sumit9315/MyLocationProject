using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Hierarchy service.
    /// </summary>
    public interface IHierarchyService
    {
        /// <summary>
        /// Retrieves the Base hierarchy structure.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="regionId">The region identifier.</param>
        /// <returns>
        /// The Base hierarchy structure
        /// </returns>
        Task<IList<HierarchyNode>> GetBaseStructureAsync(string campusId, string regionId);

        /// <summary>
        /// Gets the parents info for the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        Task<HierarchyNodeParentsInfo> GetBaseStructureParentsInfoAsync(HierarchyNodeType nodeType, string nodeId);

        /// <summary>
        /// Gets the Base Hierarchy node details.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The Base Hierarchy node details.
        /// </returns>
        Task<BaseStructureNodeInfo> GetBaseStructureNodeInfoAsync(HierarchyNodeType nodeType, string nodeId);

        /// <summary>
        /// Retrieves the Physical hierarchy structure.
        /// </summary>
        /// <returns>The Physical hierarchy structure</returns>
        Task<IList<HierarchyNode>> GetPhysicalStructureAsync();

        /// <summary>
        /// Gets the Physical child locations based on given parameters.
        /// </summary>
        /// <param name="regionId">The region identifier.</param>
        /// <param name="state">The state.</param>
        /// <param name="cityName">Name of the city.</param>
        /// <returns>
        /// The Physical child locations.
        /// </returns>
        Task<IList<HierarchyNode>> GetPhysicalLocationsAsync(string regionId, string state, string cityName);

        /// <summary>
        /// Gets the parents info for the Hierarchy in the given type.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>
        /// The parents info.
        /// </returns>
        Task<HierarchyNodeParentsInfo> GetPhysicalStructureParentsInfoAsync(string nodeId);

        /// <summary>
        /// Retrieves the management hierarchy structure.
        /// </summary>
        /// <returns>The management hierarchy structure</returns>
        Task<IList<HierarchyNode>> GetManagementStructureAsync();
    }
}
