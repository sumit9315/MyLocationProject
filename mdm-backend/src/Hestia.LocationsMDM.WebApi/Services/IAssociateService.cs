using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Associates service interface.
    /// </summary>
    public interface IAssociateService
    {
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
        Task<SearchResult<AssociateSummaryModel>> SearchAsync(
            string sortBy,
            string campusId,
            string regionId,
            string childLocationId,
            string associateId,
            string firstName,
            string lastName,
            string title,
            bool isAssociated,
            int pageNum,
            int pageSize);

        /// <summary>
        /// Gets the Associate by Id.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <returns>The Associate details.</returns>
        Task<AssociateDetailsModel> GetAsync(string associateId);

        /// <summary>
        /// Updates the Associate.
        /// </summary>
        /// <param name="associateId">The Associate Id.</param>
        /// <param name="model">The updated Associate data.</param>
        Task UpdateAsync(string associateId, AssociatePatchModel model);
    }
}
