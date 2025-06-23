using Microsoft.AspNetCore.Mvc;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Child Locations controller.
    /// </summary>
    [Route("childLocations")]
    public class ChildLocationController : BaseController
    {
        /// <summary>
        /// The child location service
        /// </summary>
        private readonly IChildLocationService _childLocationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildLocationController"/> class.
        /// </summary>
        /// <param name="childLocationService">The child location service.</param>
        public ChildLocationController(IChildLocationService childLocationService)
        {
            _childLocationService = childLocationService;
        }

        // [AllowAnonymous]
        [HttpPost("dev/testEvent")]
        public async Task<dynamic> CreateTestEvent(CalendarEventModel eventData, bool optimizePerformance, bool useMultithreading = false, int childLocCount = 0)
        {
            var res = await _childLocationService.CreateTestEvent(eventData, optimizePerformance, useMultithreading, childLocCount);
            return res;
        }

        /// <summary>
        /// Searches the child locations matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// The macthed child locations.
        /// </returns>
        [HttpGet("")]
        public async Task<SearchResult<ChildLocationSummaryModel>> SearchAsync([FromQuery]LocationSearchCriteria criteria)
        {
            criteria = criteria ?? new LocationSearchCriteria();
            var result = await _childLocationService.SearchAsync(criteria);
            return result;
        }

        /// <summary>
        /// Gets the child location by Id.
        /// </summary>
        /// <param name="locationId">The child location Id.</param>
        /// <returns>The child location details.</returns>
        [HttpGet("{locationId}")]
        public async Task<ChildLocationDetailsModel> GetAsync(string locationId)
        {
            var result = await _childLocationService.GetAsync(locationId);
            return result;
        }

        /// <summary>
        /// Gets status of the child location with given Id.
        /// </summary>
        /// <param name="childLocId">The child location identifier.</param>
        /// <returns>
        /// The child location status.
        /// </returns>
        [HttpGet("{childLocId}/status")]
        public async Task<RecordStatus> IsActiveAsync(string childLocId)
        {
            var result = await _childLocationService.GetStatusAsync(childLocId);
            return result;
        }

        /// <summary>
        /// Creates the Child Location.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created child location details.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("")]
        public async Task<ChildLocationDetailsModel> CreateAsync(ChildLocationCreateModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.LocationName, nameof(model.LocationName));
            Util.ValidateArgumentNotNullOrEmpty(model.LocationType, nameof(model.LocationType));
            Util.ValidateArgumentNotNullOrEmpty(model.LocationId, nameof(model.LocationId));
            Util.ValidateArgumentNotNullOrEmpty(model.RegionNodeId, nameof(model.RegionNodeId));

            var id = await _childLocationService.CreateAsync(model);

            var result = await _childLocationService.GetAsync(id);
            return result;
        }

        /// <summary>
        /// Performes partial update of the Child Location.
        /// </summary>
        /// <param name="locationId">The Child Location Id.</param>
        /// <param name="model">The updated Child Location data.</param>
        /// <returns>Updated Child Location details.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{locationId}")]
        public async Task<ChildLocationDetailsModel> PatchAsync(string locationId, ChildLocationPatchModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _childLocationService.UpdateAsync(locationId, model);

            // TODO: [Performance] Looks like frontend is not using updated data,
            // so either change frontend to use it (and avoid additional API requests),
            // or do not return it here

            // return updated data
            return await _childLocationService.GetAsync(locationId);
        }

        /// <summary>
        /// Deletes the child location by Id.
        /// </summary>
        /// <param name="locationId">The child location Id.</param>
        /// <returns>The No Content 204 status code.</returns>
        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{locationId}")]
        public async Task<StatusCodeResult> DeleteAsync(string locationId)
        {
            await _childLocationService.DeleteAsync(locationId);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Brands values of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Brands values.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/brands")]
        public async Task<StatusCodeResult> UpdateBrandsAsync(string node, IList<string> model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _childLocationService.UpdateBrandsAsync(node, model);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Professional Associations of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Professional Associations.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/professionalAssociations")]
        public async Task<StatusCodeResult> UpdateProfessionalAssociationsAsync(string node, IList<ProfessionalAssociationModel> items)
        {
            // validate input data
            Util.ValidateArgumentNotNull(items, nameof(items));
            var wrongItems = items.Where(x => !x.LogoSource.StartsWith('/')).ToList();
            if (wrongItems.Count > 0)
            {
                var wrongItemNames = wrongItems
                    .Select(x => x.Name)
                    .StringJoin(", ");
                throw new ArgumentException($"Logo Source must have leading slash symbol ('/'). Please fix following Professional Associations: {wrongItemNames}");
            }

            await _childLocationService.UpdateProfessionalAssociationsAsync(node, items);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Merchandising Banners of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Merchandising Banners.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/merchandisingBanners")]
        public async Task<StatusCodeResult> UpdateMerchandisingBannersAsync(string node, IList<MerchandisingBannerModel> items)
        {
            Util.ValidateArgumentNotNull(items, nameof(items));

            await _childLocationService.UpdateMerchandisingBannersAsync(node, items);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Branch Additional Content of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="data">The updated Branch Additional Content.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/branchAdditionalContent")]
        public async Task<StatusCodeResult> UpdateBranchAdditionalContentAsync(string node, ChildBranchAdditionalContentModel data)
        {
            Util.ValidateArgumentNotNull(data, nameof(data));

            await _childLocationService.UpdateBranchAdditionalContentAsync(node, data);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Financial Data items of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Financial Data items.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/financialData")]
        public async Task<StatusCodeResult> UpdateFinancialDataAsync(string node, IList<FinancialDataItem> model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            await _childLocationService.UpdateFinancialDataAsync(node, model);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates Business Info of the given Child Location.
        /// </summary>
        /// <param name="locationId">The Child Location node Id.</param>
        /// <param name="model">The updated Business Info data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPatch("{locationId}/businessInfo")]
        public async Task<StatusCodeResult> PatchBusinessInfoAsync(string locationId, ChildLocBusinessInfoModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            // when 'Text To Counter' is ON, it must also have 'Text To Counter Phone Number'
            if (model.TextToCounter == true && string.IsNullOrWhiteSpace(model.TextToCounterPhoneNumber))
            {
                throw new ArgumentException("'Text to Counter Phone Number must be provided when Text to Counter is turned ON.'");
            }

            await _childLocationService.UpdateBusinessInfoAsync(locationId, model);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates the Operating Hours of given Child Location.
        /// </summary>
        /// <param name="locationId">The Child Location node Id.</param>
        /// <param name="model">The updated Operating Hours data.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{locationId}/operatingHours")]
        public async Task<StatusCodeResult> UpdateOperatingHoursAsync(string locationId, OperatingHoursUpdateModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));

            foreach (var operatingHoursModel in model.OperatingHours)
            {
                Util.ValidateArgumentNotNull(operatingHoursModel.DayOfWeek, "DayOfWeek");
                Util.ValidateArgumentNotNullOrEmpty(operatingHoursModel.OpenCloseFlag, "OpenCloseFlag");
                if (operatingHoursModel.OpenCloseFlag == "Open")
                {
                    Util.ValidateArgumentValidTimeGreater(operatingHoursModel.OpenHour, operatingHoursModel.CloseHour,
                        "OpenHour", "CloseHour", @"hh\:mm");

                    if (!string.IsNullOrEmpty(operatingHoursModel.OpenAfterHour))
                    {
                        Util.ValidateArgumentValidTimeGreater(operatingHoursModel.CloseHour, operatingHoursModel.OpenAfterHour,
                            "CloseHour", "OpenAfterHour", @"hh\:mm");
                        Util.ValidateArgumentValidTimeGreater(operatingHoursModel.OpenAfterHour, operatingHoursModel.CloseAfterHour,
                            "OpenAfterHour", "CloseAfterHour", @"hh\:mm");
                    }
                    if (!string.IsNullOrEmpty(operatingHoursModel.OpenReceivingHour))
                    {
                        Util.ValidateArgumentValidTimeGreater(operatingHoursModel.OpenReceivingHour, operatingHoursModel.CloseReceivingHour,
                            "OpenReceivingHour", "CloseReceivingHour", @"hh\:mm");
                    }
                }
            }

            await _childLocationService.UpdateOperatingHoursAsync(locationId, model);

            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Assigns Associates to the given Child Location.
        /// </summary>
        /// <param name="locationId">The Child Location Id.</param>
        /// <param name="model">The assign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{locationId}/contactRoles/assign")]
        public async Task<StatusCodeResult> AssignAssociatesAsync(string locationId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _childLocationService.AssignAssociatesAsync(locationId, model, AssignMode.Assign);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Unassigns Associates from the given Child Location.
        /// </summary>
        /// <param name="locationId">The Child Location Id.</param>
        /// <param name="model">The unassign associates model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{locationId}/contactRoles/unassign")]
        public async Task<StatusCodeResult> UnassignAssociatesAsync(string locationId, AssignAssociatesModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.ContactList, nameof(model.ContactList));

            await _childLocationService.AssignAssociatesAsync(locationId, model, AssignMode.Unassign);
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
            var result = await _childLocationService.GetEventsAsync(NodeType.ChildLoc, node);
            return result;
        }

        /// <summary>
        /// Updates events for the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Node.</param>
        /// <param name="model">The events details model.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/events")]
        public async Task<StatusCodeResult> UpdateEventsAsync(string node, LocationEventsModel model)
        {
            ValidateEventsModel(model);

            await _childLocationService.UpdateEventsAsync(NodeType.ChildLoc, node, model);
            return StatusCode((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates Location Type and KOB for the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Node.</param>
        /// <param name="model">The updated location type details.</param>
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{node}/locationType")]
        public async Task<dynamic> UpdateLocationTypeAsync(string node, UpdateLocationTypeModel model)
        {
            Util.ValidateArgumentNotNullOrEmpty(node, nameof(node));
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNullOrEmpty(model.LocationType, nameof(model.LocationType));

            var newNode = await _childLocationService.UpdateLocationTypeAsync(node, model);

            return new
            {
                NewNode = newNode
            };
        }
    }
}
