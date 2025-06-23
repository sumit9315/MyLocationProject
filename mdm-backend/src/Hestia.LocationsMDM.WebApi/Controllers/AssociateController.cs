using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Associate controller.
    /// </summary>
    [Route("associates")]
    public class AssociateController : BaseController
    {
        /// <summary>
        /// The Associate service.
        /// </summary>
        private readonly IAssociateService _associateService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssociateController"/> class.
        /// </summary>
        /// <param name="associateService">The associate service.</param>
        public AssociateController(IAssociateService associateService)
        {
            _associateService = associateService;
        }

        /// <summary>
        /// Searches the associates matching given criteria.
        /// </summary>
        /// <param name="sortBy">The sort by criteria.</param>
        /// <param name="campusId">The campus Id criteria.</param>
        /// <param name="regionId">The region Id criteria.</param>
        /// <param name="childLocationId">The child location Id criteria.</param>
        /// <param name="associateId">The associate Id criteria.</param>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="title">The title.</param>
        /// <param name="isAssociated">If filter associated only.</param>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>
        /// The macthed associates.
        /// </returns>
        [HttpGet("")]
        public async Task<SearchResult<AssociateSummaryModel>> SearchAsync(
            string sortBy = "associateId",
            string campusId = null,
            string regionId = null,
            string childLocationId = null,
            string associateId = null,
            string firstName = null,
            string lastName = null,
            string title = null,
            bool isAssociated = true,
            int pageNum = 1,
            int pageSize = -1)
        {
            var result = await _associateService.SearchAsync(
                sortBy,
                campusId,
                regionId,
                childLocationId,
                associateId,
                firstName,
                lastName,
                title,
                isAssociated,
                pageNum,
                pageSize);

            return result;
        }

        /// <summary>
        /// Gets the Associate by Id.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <returns>The Associate details.</returns>
        [HttpGet("{associateId}")]
        public async Task<AssociateDetailsModel> GetAsync(string associateId)
        {
            var result = await _associateService.GetAsync(associateId);
            return result;
        }

        /// <summary>
        /// Performes partial update of the Associate.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <param name="model">The updated Associate data.</param>
        /// <returns>Updated Associate details.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{associateId}")]
        public async Task<AssociateDetailsModel> PatchAsync(string associateId, AssociatePatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _associateService.UpdateAsync(associateId, model);

            // return updated data
            return await _associateService.GetAsync(associateId);
        }
    }
}
