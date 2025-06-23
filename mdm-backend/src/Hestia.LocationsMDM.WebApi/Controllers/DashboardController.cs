using Microsoft.AspNetCore.Mvc;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Dashboard controller.
    /// </summary>
    [Route("")]
    public class DashboardController : BaseController
    {
        /// <summary>
        /// The Dashboard service.
        /// </summary>
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Gets the Dashboard statistics.
        /// </summary>
        /// <returns>The Dashboard statistics.</returns>
        [HttpGet("dashboardInfo")]
        public async Task<DashboardStatisticsModel> GetStatisticsAsync()
        {
            var result = await _dashboardService.GetStatisticsAsync();
            return result;
        }
    }
}
