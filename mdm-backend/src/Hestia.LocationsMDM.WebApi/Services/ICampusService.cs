using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Campus service interface.
    /// </summary>
    public interface ICampusService : ILocationService
    {
        /// <summary>
        /// Gets the campus details by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>The campus details or null if not exists.</returns>
        Task<CampusDetailsModel> GetCampusAsync(string campusId);

        /// <summary>
        /// Updates the Campus.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <param name="model">The updated Campus data.</param>
        Task UpdateCampusAsync(string campusId, CampusPatchModel model);

        /// <summary>
        /// Gets the roles of the Campus with the given Id.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <returns>
        /// The roles of the Campus.
        /// </returns>
        Task<IList<CampusRoleModel>> GetCampusRolesAsync(string campusId);

        /// <summary>
        /// Creates the campus role.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="roleName">Name of the role.</param>
        Task CreateCampusRoleAsync(string campusId, string roleName);

        /// <summary>
        /// Assigns or Unassigns Associates with the given Campus.
        /// </summary>
        /// <param name="campusId">The campus identifier.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        Task AssignCampusAssociatesAsync(string campusId, AssignAssociatesModel model, AssignMode assignMode);
    }
}
