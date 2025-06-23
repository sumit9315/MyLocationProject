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
    /// The Regions controller.
    /// </summary>
    [Route("regions")]
    public class RegionController : BaseController
    {
        /// <summary>
        /// The Region service.
        /// </summary>
        private readonly IRegionService _regionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionController" /> class.
        /// </summary>
        /// <param name="regionService">The region service.</param>
        public RegionController(IRegionService regionService)
        {
            _regionService = regionService;
        }

        /// <summary>
        /// Gets the region details by Id.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <returns>
        /// The region details.
        /// </returns>
        [HttpGet("{regionId}")]
        public async Task<RegionDetailsModel> GetAsync(string regionId)
        {
            var result = await _regionService.GetAsync(regionId);
            return result;
        }

        /// <summary>
        /// Updates the Region.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <param name="model">The updated Region data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{regionId}")]
        public async Task<RegionDetailsModel> UpdateAsync(string regionId, RegionPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _regionService.UpdateAsync(regionId, model);

            // return updated data
            return await _regionService.GetAsync(regionId);
        }

        /// <summary>
        /// Performes partial update of the Region.
        /// </summary>
        /// <param name="regionId">The region Id.</param>
        /// <param name="model">The updated Region data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{regionId}")]
        public async Task<RegionDetailsModel> PatchAsync(string regionId, RegionPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _regionService.UpdateAsync(regionId, model);

            // return updated data
            return await _regionService.GetAsync(regionId);
        }

        /// <summary>
        /// Assigns Associates to the given Region.
        /// </summary>
        /// <param name="regionId">The Region Id.</param>
        /// <param name="model">The assign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{regionId}/contactRoles/assign")]
        public async Task<StatusCodeResult> AssignAssociatesAsync(string regionId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _regionService.AssignAssociatesAsync(regionId, model, AssignMode.Assign);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Unassigns Associates from the given Region.
        /// </summary>
        /// <param name="regionId">The Region Id.</param>
        /// <param name="model">The unassign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{regionId}/contactRoles/unassign")]
        public async Task<StatusCodeResult> UnassignAssociatesAsync(string regionId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _regionService.AssignAssociatesAsync(regionId, model, AssignMode.Unassign);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Gets events for the given Campus.
        /// </summary>
        /// <param name="node">The Campus Node.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{node}/events")]
        public async Task<LocationCalendarEventsModel> GetEventsAsync(string node)
        {
            var result = await _regionService.GetEventsAsync(NodeType.Region, node);
            return result;
        }

        /// <summary>
        /// Updates events for the given Region.
        /// </summary>
        /// <param name="node">The Region Node.</param>
        /// <param name="model">The events details model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/events")]
        public async Task<StatusCodeResult> UpdateEventsAsync(string node, LocationEventsModel model)
        {
            ValidateEventsModel(model);

            await _regionService.UpdateEventsAsync(NodeType.Region, node, model);
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
