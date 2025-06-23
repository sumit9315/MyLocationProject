using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using System;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Change History controller.
    /// </summary>
    [Route("")]
    public class ChangeHistoryController : BaseController
    {
        /// <summary>
        /// The Change History service.
        /// </summary>
        private readonly IChangeHistoryService _changeHistoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHistoryController"/> class.
        /// </summary>
        /// <param name="changeHistoryService">The child location service.</param>
        public ChangeHistoryController(IChangeHistoryService changeHistoryService)
        {
            _changeHistoryService = changeHistoryService;
        }

        /// <summary>
        /// Searches the change history summary matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change summary for the given criteria.
        /// </returns>
        [HttpGet("changeHistorySummary")]
        public async Task<SearchResult<LocationChangeSummary>> SearchChangeHistorySummayAsync([FromQuery]ChangeSummarySearchCriteria criteria)
        {
            criteria ??= new ChangeSummarySearchCriteria();
            if (string.IsNullOrEmpty(criteria.SortBy))
            {
                criteria.SortBy = "lastChangedDate";
                criteria.SortOrder = SortOrder.Desc;
            }

            var result = await _changeHistoryService.SearchChangeSummariesAsync(criteria);
            return result;
        }

        /// <summary>
        /// Searches the change history matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change history matching given criteria.
        /// </returns>
        [HttpPost("changeHistory/search")]
        public async Task<SearchResult<ObjectChangeModel>> SearchChangeHistoryAsync(ChangeHistorySearchCriteria criteria)
        {
            criteria ??= new ChangeHistorySearchCriteria();
            if (string.IsNullOrEmpty(criteria.SortBy))
            {
                criteria.SortBy = "changedOn";
                criteria.SortOrder = SortOrder.Desc;
            }

            var result = await _changeHistoryService.SearchChangeHistoryAsync(criteria);
            return result;
        }
    }
}
