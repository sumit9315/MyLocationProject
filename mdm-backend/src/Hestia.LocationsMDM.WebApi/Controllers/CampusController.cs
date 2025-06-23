using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Campus controller.
    /// </summary>
    [Route("campuses")]
    public class CampusController : BaseController
    {
        /// <summary>
        /// The Campus service.
        /// </summary>
        private readonly ICampusService _campusService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampusController" /> class.
        /// </summary>
        /// <param name="campusService">The campus service.</param>
        /// <param name="memoryCache">The memory cache.</param>
        public CampusController(ICampusService campusService, IMemoryCache memoryCache)
            : base(memoryCache)
        {
            _campusService = campusService;
        }

        /// <summary>
        /// Gets the campus details by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>The campus details.</returns>
        [HttpGet("{campusId}")]
        public async Task<CampusDetailsModel> GetCampusAsync(string campusId)
        {
            var result = await _campusService.GetCampusAsync(campusId);
            return result;
        }

        /// <summary>
        /// Updates the Campus.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <param name="model">The updated Campus data.</param>
        /// <returns>Updated Campus details.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{campusId}")]
        public async Task<CampusDetailsModel> UpdateCampusAsync(string campusId, CampusPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _campusService.UpdateCampusAsync(campusId, model);

            // update name in cache
            if (model.LocationName != null)
            {
                UpdateCampusNameInBaseHierarchyCache(campusId, model.LocationName);
            }

            // return updated data
            return await _campusService.GetCampusAsync(campusId);
        }

        /// <summary>
        /// Performes partial update of the Campus.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <param name="model">The updated Campus data.</param>
        /// <returns>Updated Campus details.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{campusId}")]
        public async Task<CampusDetailsModel> PatchCampusAsync(string campusId, CampusPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _campusService.UpdateCampusAsync(campusId, model);

            // return updated data
            return await _campusService.GetCampusAsync(campusId);
        }

        /// <summary>
        /// Gets the roles of the Campus with the given Id.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <returns>
        /// The roles of the Campus.
        /// </returns>
        [HttpGet("{campusId}/roles")]
        public async Task<IList<CampusRoleModel>> GetCampusRolesAsync(string campusId)
        {
            var result = await _campusService.GetCampusRolesAsync(campusId);
            return result;
        }

        /// <summary>
        /// Creates the campus role.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The campus role model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{campusId}/roles")]
        public async Task<StatusCodeResult> CreateCampusRoleAsync(string campusId, CampusRoleCreateModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.RoleName, nameof(model.RoleName));

            await _campusService.CreateCampusRoleAsync(campusId, model.RoleName);
            return StatusCode((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Assigns Associates to the given Campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The assign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{campusId}/contactRoles/assign")]
        public async Task<StatusCodeResult> AssignCampusAssociatesAsync(string campusId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _campusService.AssignCampusAssociatesAsync(campusId, model, AssignMode.Assign);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Unassigns Associates to the given Campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The unassign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{campusId}/contactRoles/unassign")]
        public async Task<StatusCodeResult> UnassignCampusAssociatesAsync(string campusId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _campusService.AssignCampusAssociatesAsync(campusId, model, AssignMode.Unassign);
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
            var result = await _campusService.GetEventsAsync(NodeType.Campus, node);
            return result;
        }

        /// <summary>
        /// Updates events for the given Campus.
        /// </summary>
        /// <param name="node">The Campus Node.</param>
        /// <param name="model">The events details model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/events")]
        public async Task<StatusCodeResult> UpdateEventsAsync(string node, LocationEventsModel model)
        {
            ValidateEventsModel(model);

            await _campusService.UpdateEventsAsync(NodeType.Campus, node, model);
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
