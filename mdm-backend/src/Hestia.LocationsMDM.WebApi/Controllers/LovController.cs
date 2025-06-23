using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The List of Values management controller.
    /// </summary>
    [Route("lov")]
    public class LovController : BaseController
    {
        /// <summary>
        /// The Calendar Event service.
        /// </summary>
        private readonly ILovService _lovService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LovController" /> class.
        /// </summary>
        /// <param name="lovService">The calendar event service.</param>
        public LovController(ILovService lovService)
        {
            _lovService = lovService;
        }

        /// <summary>
        /// Searches the list of values matching given criteria.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sortBy">The sort by.</param>
        /// <param name="pageNum">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>
        /// The macthed list of values.
        /// </returns>
        [HttpGet("")]
        public async Task<SearchResult<LovItemModel>> SearchAsync(string key, string sortBy = "sequence", int pageNum = 1, int pageSize = 1000)
        {
            var result = await _lovService.SearchAsync(key, sortBy, pageNum, pageSize);

            return result;
        }

        /// <summary>
        /// Gets the list of all KOB values.
        /// </summary>
        /// <returns>
        /// The list of KOB values.
        /// </returns>
        [HttpGet("kobs")]
        public async Task<IList<string>> GetAllKOBsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<string>("KOB");
            return result;
        }

        /// <summary>
        /// Gets the list of all Locker PU values.
        /// </summary>
        /// <returns>
        /// The list of Locker PU values.
        /// </returns>
        [HttpGet("lockerPUs")]
        public async Task<IList<LovLookupModel>> GetAllLockerPUsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<LovLookupModel>("lockerPU");
            return result;
        }

        /// <summary>
        /// Gets the list of all Cages values.
        /// </summary>
        /// <returns>
        /// The list of Cages values.
        /// </returns>
        [HttpGet("cages")]
        public async Task<IList<LovLookupModel>> GetAllCagesAsync()
        {
            var result = await _lovService.GetAllValuesAsync<LovLookupModel>("cages");
            return result;
        }

        /// <summary>
        /// Gets the list of all Professional Association values.
        /// </summary>
        /// <returns>
        /// The list of Professional Association values.
        /// </returns>
        [HttpGet("professionalAssociations")]
        public async Task<IList<ProfessionalAssociationLovModel>> GetAllProfessionalAssociationsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<ProfessionalAssociationLovModel>("professionalAssociations");
            return result;
        }

        /// <summary>
        /// Gets the list of all Services and Certification values.
        /// </summary>
        /// <returns>
        /// The list of Services and Certification values.
        /// </returns>
        [HttpGet("servicesAndCertifications")]
        public async Task<IList<string>> GetAllServicesAndSertificationsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<string>("servicesAndCertifications");
            return result;
        }

        /// <summary>
        /// Gets the list of all Product Offering values.
        /// </summary>
        /// <returns>
        /// The list of Product Offering values.
        /// </returns>
        [HttpGet("productOfferings")]
        public async Task<IList<string>> GetAllProductOfferingsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<string>("productOfferings");
            return result;
        }

        /// <summary>
        /// Gets the list of all Brand values.
        /// </summary>
        /// <returns>
        /// The list of Brand values.
        /// </returns>
        [HttpGet("brands")]
        public async Task<IList<string>> GetAllBrandsAsync()
        {
            var result = await _lovService.GetAllValuesAsync<string>("brands");
            return result;
        }

        /// <summary>
        /// Gets the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <returns>
        /// The List of Value item details.
        /// </returns>
        [HttpGet("{valueId}")]
        public async Task<LovItemModel> GetAsync(string valueId)
        {
            var result = await _lovService.GetAsync(valueId);
            return result;
        }

        /// <summary>
        /// Creates the List of Value item.
        /// </summary>
        /// <param name="model">The List of Value item data.</param>
        /// <returns>Created List of Value item model.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("")]
        public async Task<LovItemModel> CreateAsync(LovItemModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            // all properties are required
            Util.ValidateArgumentNotNullOrEmpty(model.Key, nameof(model.Key));
            Util.ValidateArgumentNotNullOrEmpty(model.Value, nameof(model.Value));

            var createdModel = await _lovService.CreateAsync(model);
            return createdModel;
        }

        /// <summary>
        /// Updates the List of Value item.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <param name="model">The updated List of Value item data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{valueId}")]
        public async Task<LovItemModel> UpdateAsync(string valueId, LovItemPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            // all properties are required
            Util.ValidateArgumentNotNullOrEmpty(model.Key, nameof(model.Key));
            Util.ValidateArgumentNotNullOrEmpty(model.Value, nameof(model.Value));
            Util.ValidateArgumentNotNull(model.Sequence, nameof(model.Sequence));

            await _lovService.UpdateAsync(valueId, model);

            // return updated data
            return await _lovService.GetAsync(valueId);
        }

        /// <summary>
        /// Performes partial update of the List of Value item.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <param name="model">The updated List of Value item properties.</param>
        /// <returns>
        /// Updated List of Value item details.
        /// </returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{valueId}")]
        public async Task<LovItemModel> PatchAsync(string valueId, LovItemPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _lovService.UpdateAsync(valueId, model);

            // return updated data
            return await _lovService.GetAsync(valueId);
        }

        /// <summary>
        /// Deletes the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <returns>
        /// The No Content 204 status code.
        /// </returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{valueId}")]
        public async Task<StatusCodeResult> DeleteAsync(string valueId)
        {
            await _lovService.DeleteAsync(valueId);
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}
