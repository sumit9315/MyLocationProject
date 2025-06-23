using Hestia.LocationsMDM.WebApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Lookup service interface.
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Finds the financial data by given parameters.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <param name="costCenterId">The cost center Id.</param>
        /// <param name="lobCc">The lob CC.</param>
        /// <returns>Found Financial Data, or null if not exists.</returns>
        Task<FinancialDataItem> FindFinancialDataAsync(string childLocNode, string costCenterId = null, string lobCc = null);

        /// <summary>
        /// Gets the available financial data for the given child location node.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <returns>Available Financial Data lookups, or empty list if not exists.</returns>
        Task<IList<FinancialDataItem>> GetAvailableFinancialDatasAsync(string childLocNode);

        /// <summary>
        /// Gets all available Location IDs for the given region.
        /// </summary>
        /// <returns>All available Location IDs.</returns>
        Task<IList<string>> GetRegionLocationIdsAsync(string regionNode);

        /// <summary>
        /// Gets all unique region names.
        /// </summary>
        /// <returns>All unique region names.</returns>
        Task<IList<string>> GetRegionNamesAsync();

        /// <summary>
        /// Gets the distinct Trilogie Logon values from EDMCS.
        /// </summary>
        /// <returns>Distinct Trilogie Logon values.</returns>
        Task<IList<string>> GetDistinctTrilogieLogonsFromEdmcsAsync();

        /// <summary>
        /// Gets all distinct Pricing Region values from Pricing Region mapping.
        /// </summary>
        /// <returns>Distinct Pricing Region values.</returns>
        Task<IList<string>> GetPricingRegionsValuesAsync();

        /// <summary>
        /// Gets all cities.
        /// </summary>
        /// <returns>All unique cities from locations.</returns>
        Task<IList<CityLookupModel>> GetCities();

        /// <summary>
        /// Gets all districts.
        /// </summary>
        /// <returns>All unique districts from locations.</returns>
        Task<IList<LookupModel>> GetDistricts();

        /// <summary>
        /// Gets all unique location types.
        /// </summary>
        /// <returns>All unique location types.</returns>
        Task<IList<string>> GetLocationTypesAsync();

        /// <summary>
        /// Gets the Campus lookups.
        /// </summary>
        /// <returns>
        /// The Campus lookups.
        /// </returns>
        Task<IList<LocationLookupModel>> GetAllCampusesAsync();

        /// <summary>
        /// Gets the region lookups of the Campus by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>
        /// The region lookups.
        /// </returns>
        Task<IList<LookupModel>> GetCampusRegionLookupsAsync(string campusId);

        /// <summary>
        /// Gets the state lookups.
        /// </summary>
        /// <returns>
        /// The state lookups.
        /// </returns>
        Task<IList<LookupModel>> GetStatesAsync();

        /// <summary>
        /// Gets the countries with corresponding states.
        /// </summary>
        /// <returns>
        /// The countries with corresponding states.
        /// </returns>
        Task<IList<CountryStatesModel>> GetCountryStatesAsync();

        /// <summary>
        /// Gets the child location lookups of the Region by Id.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <returns>
        /// The child location lookups.
        /// </returns>
        Task<IList<ChildLocationLookupModel>> GetChildLocationLookupsAsync(string regionNodeId);
    }
}
