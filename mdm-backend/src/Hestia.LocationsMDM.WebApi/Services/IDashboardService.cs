using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Dashboard service.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Gets the Dashboard statistics.
        /// </summary>
        /// <returns>The Dashboard statistics.</returns>
        Task<DashboardStatisticsModel> GetStatisticsAsync();
    }
}
