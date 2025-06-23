using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Pricing Region controller.
    /// </summary>
    [Route("pricingRegion")]
    public class PricingRegionController : BaseController
    {
        /// <summary>
        /// The Pricing Region service.
        /// </summary>
        private readonly IPricingRegionService _pricingRegionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PricingRegionController" /> class.
        /// </summary>
        /// <param name="pricingRegionService">The pricing region service.</param>
        public PricingRegionController(IPricingRegionService pricingRegionService)
        {
            _pricingRegionService = pricingRegionService;
        }

        /// <summary>
        /// Gets the Pricing Region Mappings.
        /// </summary>
        /// <returns>The Pricing Region Mappings.</returns>
        [HttpGet("mappings")]
        public async Task<IList<PricingRegionMappingModel>> GetPricingRegionMappingsAsync()
        {
            var result = await _pricingRegionService.GetPricingRegionMappingsAsync();
            return result;
        }

        /// <summary>
        /// Creates new Pricing Region mapping.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created model Id.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("mappings")]
        public async Task<PricingRegionMappingModel> CreatePricingRegionMappingsAsync(PricingRegionMappingCreateModel model)
        {
            var result = await _pricingRegionService.CreateMappingAsync(model);
            return result;
        }

        /// <summary>
        /// Deletes the Pricing Region mapping by Id.
        /// </summary>
        /// <param name="pricingRegionId">The Pricing Region Id.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("mappings/{pricingRegionId}")]
        public async Task DeleteMappingAsync(string pricingRegionId)
        {
            await _pricingRegionService.DeleteMappingAsync(pricingRegionId);
        }

        /// <summary>
        /// Updates the pricing region mapping.
        /// </summary>
        /// <param name="pricingRegionId">The pricing region Id.</param>
        /// <param name="model">The updated Pricing Region mapping data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("mappings/{pricingRegionId}")]
        public async Task UpdateMappingAsync(string pricingRegionId, PricingRegionMappingCreateModel model)
        {
            await _pricingRegionService.UpdateMappingAsync(pricingRegionId, model);
        }
    }
}
