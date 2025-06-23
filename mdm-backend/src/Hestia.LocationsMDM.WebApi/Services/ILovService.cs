using Hestia.LocationsMDM.WebApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The List of Values service interface.
    /// </summary>
    public interface ILovService
    {
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
        Task<SearchResult<LovItemModel>> SearchAsync(
            string key,
            string sortBy,
            int pageNum,
            int pageSize);

        /// <summary>
        /// Gets the list of values for the given Key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// The list of values.
        /// </returns>
        Task<IList<T>> GetAllValuesAsync<T>(string key);

        /// <summary>
        /// Gets the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <returns>
        /// The List of Value item details.
        /// </returns>
        Task<LovItemModel> GetAsync(string valueId);

        /// <summary>
        /// Creates the List of Value item.
        /// </summary>
        /// <param name="model">The List of Value item data.</param>
        /// <returns>Created List of Value item model.</returns>
        Task<LovItemModel> CreateAsync(LovItemModel model);

        /// <summary>
        /// Updates the List of Value item.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        /// <param name="model">The updated List of Value item data.</param>
        Task UpdateAsync(string valueId, LovItemPatchModel model);

        /// <summary>
        /// Deletes the List of Value item by Id.
        /// </summary>
        /// <param name="valueId">The List of Value item Id.</param>
        Task DeleteAsync(string valueId);
    }
}
