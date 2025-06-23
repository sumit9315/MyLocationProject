using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Child Location service interface.
    /// </summary>
    public interface IChildLocationService : ILocationService
    {
        Task<dynamic> CreateTestEvent(CalendarEventModel eventData, bool optimizePerformance, bool useMultithreading, int childLocCount);


        /// <summary>
        /// Searches the child locations matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// The macthed child locations.
        /// </returns>
        Task<SearchResult<ChildLocationSummaryModel>> SearchAsync(LocationSearchCriteria criteria);

        /// <summary>
        /// Gets the child location by Id.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        /// <returns>The child location details.</returns>
        Task<ChildLocationDetailsModel> GetAsync(string node);

        /// <summary>
        /// Gets status of the child location with given Id.
        /// </summary>
        /// <param name="childLocId">The child location identifier.</param>
        /// <returns>
        /// The child location status.
        /// </returns>
        Task<RecordStatus> GetStatusAsync(string childLocId);

        /// <summary>
        /// Creates the Child Location.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Created child location details.</returns>
        Task<string> CreateAsync(ChildLocationCreateModel model);

        /// <summary>
        /// Updates the Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Child Location data.</param>
        Task UpdateAsync(string node, ChildLocationPatchModel model);

        /// <summary>
        /// Deletes the child location by Id.
        /// </summary>
        /// <param name="node">The child location Id.</param>
        Task DeleteAsync(string node);

        /// <summary>
        /// Updates the Brands values of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Brands values.</param>
        Task UpdateBrandsAsync(string node, IList<string> model);

        /// <summary>
        /// Updates the Professional Associations of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Professional Associations.</param>
        Task UpdateProfessionalAssociationsAsync(string node, IList<ProfessionalAssociationModel> items);

        /// <summary>
        /// Updates the Merchandising Banners of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="items">The updated Merchandising Banners.</param>
        Task UpdateMerchandisingBannersAsync(string node, IList<MerchandisingBannerModel> items);

        /// <summary>
        /// Updates the Branch Additional Content of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="data">The updated Branch Additional Content.</param>
        Task UpdateBranchAdditionalContentAsync(string node, ChildBranchAdditionalContentModel data);

        /// <summary>
        /// Updates the Financial Data items of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Financial Data items.</param>
        Task UpdateFinancialDataAsync(string node, IList<FinancialDataItem> model);

        /// <summary>
        /// Updates the Business Info of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Business Info data.</param>
        Task UpdateBusinessInfoAsync(string node, ChildLocBusinessInfoModel model);

        /// <summary>
        /// Updates the Operating Hours of given Child Location.
        /// </summary>
        /// <param name="node">The Child Location node Id.</param>
        /// <param name="model">The updated Operating Hours data.</param>
        Task UpdateOperatingHoursAsync(string node, OperatingHoursUpdateModel model);

        /// <summary>
        /// Assigns or Unassigns Associates with the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Id.</param>
        /// <param name="model">The assign associates model.</param>
        /// <param name="assignMode">The assign mode.</param>
        Task AssignAssociatesAsync(string node, AssignAssociatesModel model, AssignMode assignMode);

        /// <summary>
        /// Updates Location Type and KOB for the given Child Location.
        /// </summary>
        /// <param name="node">The Child Location Node.</param>
        /// <param name="model">The updated location type details.</param>
        /// <returns>Node of the updated Child Location.</returns>
        Task<string> UpdateLocationTypeAsync(string node, UpdateLocationTypeModel model);
    }
}
