using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Pricing Region service interface.
    /// </summary>
    public interface IPricingRegionService
    {
        /// <summary>
        /// Gets the Pricing Region Mappings.
        /// </summary>
        /// <returns>The Pricing Region Mappings.</returns>
        Task<IList<PricingRegionMappingModel>> GetPricingRegionMappingsAsync();

        /// <summary>
        /// Gets the Pricing Region by Trilogie Logon.
        /// </summary>
        /// <returns>The Pricing Region.</returns>
        Task<string> GetPricingRegionAsync(string trilogieLogon);

        /// <summary>
        /// Creates new Pricing Region mapping.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created model Id.</returns>
        Task<PricingRegionMappingModel> CreateMappingAsync(PricingRegionMappingCreateModel model);

        /// <summary>
        /// Deletes the Pricing Region mapping by Id.
        /// </summary>
        /// <param name="pricingRegionId">The Pricing Region Id.</param>
        Task DeleteMappingAsync(string pricingRegionId);

        /// <summary>
        /// Updates the pricing region mapping.
        /// </summary>
        /// <param name="pricingRegionId">The pricing region Id.</param>
        /// <param name="model">The updated Pricing Region mapping data.</param>
        Task UpdateMappingAsync(string pricingRegionId, PricingRegionMappingCreateModel model);
    }
}
