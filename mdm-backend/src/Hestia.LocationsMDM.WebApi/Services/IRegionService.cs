using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Region service interface.
    /// </summary>
    public interface IRegionService : ILocationService
    {
        /// <summary>
        /// Gets the Region details by Id.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <returns>
        /// The region details.
        /// </returns>
        Task<RegionDetailsModel> GetAsync(string regionId);

        /// <summary>
        /// Updates the region with given Id.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <param name="model">The updated data.</param>
        Task UpdateAsync(string regionId, RegionPatchModel model);

        /// <summary>
        /// Assigns or Unassigns Associates with the given Region.
        /// </summary>
        /// <param name="regionId">The Region Id.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        Task AssignAssociatesAsync(string regionId, AssignAssociatesModel model, AssignMode assignMode);
    }
}
