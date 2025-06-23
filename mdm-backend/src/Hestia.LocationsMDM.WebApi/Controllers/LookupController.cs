using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Hestia.LocationsMDM.WebApi.Services;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Common;
using System.Net;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The Lookup controller.
    /// </summary>
    [Route("lookup")]
    public class LookupController : BaseController
    {
        /// <summary>
        /// The lookup service
        /// </summary>
        private readonly ILookupService _lookupService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupController"/> class.
        /// </summary>
        /// <param name="lookupService">The child location service.</param>
        public LookupController(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        /// <summary>
        /// Finds the financial data by given parameters.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <param name="costCenterId">The cost center Id.</param>
        /// <param name="lobCc">The lob CC.</param>
        /// <returns>Found Financial Data, or null if not exists.</returns>
        [HttpGet("financialData")]
        public async Task<FinancialDataItem> FindFinancialDataAsync(string childLocNode, string costCenterId = null, string lobCc = null)
        {
            Util.ValidateArgumentNotNullOrEmpty(childLocNode, nameof(childLocNode));

            if (string.IsNullOrEmpty(costCenterId) && string.IsNullOrEmpty(lobCc))
            {
                throw new ArgumentException("Either costCenterId or lobCc must be provided.");
            }

            var result = await _lookupService.FindFinancialDataAsync(childLocNode, costCenterId, lobCc);
            return result;
        }

        /// <summary>
        /// Gets the available financial data for the given child location node.
        /// </summary>
        /// <param name="childLocNode">The child location node.</param>
        /// <returns>Available Financial Data lookups, or empty list if not exists.</returns>
        [HttpGet("financialData/available")]
        public async Task<IList<FinancialDataItem>> GetAvailableFinancialDataAsync(string childLocNode)
        {
            Util.ValidateArgumentNotNullOrEmpty(childLocNode, nameof(childLocNode));

            var result = await _lookupService.GetAvailableFinancialDatasAsync(childLocNode);
            return result;
        }

        /// <summary>
        /// Gets all available Location IDs for the given region.
        /// </summary>
        /// <returns>All available Location IDs.</returns>
        [HttpGet("regions/{regionNode}/locationIDs")]
        public async Task<IList<string>> GetRegionLocationIdsAsync(string regionNode)
        {
            Util.ValidateArgumentNotNullOrEmpty(regionNode, nameof(regionNode));

            var result = await _lookupService.GetRegionLocationIdsAsync(regionNode);
            return result;
        }

        /// <summary>
        /// Gets all unique region names.
        /// </summary>
        /// <returns>All unique region names.</returns>
        [HttpGet("regionNames")]
        public async Task<IList<string>> GetRegionNamesAsync()
        {
            var result = await _lookupService.GetRegionNamesAsync();
            return result;
        }

        /// <summary>
        /// Gets all distinct Pricing Region values from Pricing Region mapping.
        /// </summary>
        /// <returns>Distinct Pricing Region values.</returns>
        [HttpGet("pricingRegionValues")]
        public async Task<IList<string>> GetPricingRegionValuesAsync()
        {
            var result = await _lookupService.GetPricingRegionsValuesAsync();
            return result;
        }

        /// <summary>
        /// Gets all cities.
        /// </summary>
        /// <returns>All unique cities from locations.</returns>
        [HttpGet("cities")]
        public async Task<IList<CityLookupModel>> GetCities()
        {
            var result = await _lookupService.GetCities();
            return result;
        }

        /// <summary>
        /// Gets all districts.
        /// </summary>
        /// <returns>All unique districts from locations.</returns>
        [HttpGet("districts")]
        public async Task<IList<LookupModel>> GetDistricts()
        {
            var result = await _lookupService.GetDistricts();
            return result;
        }

        /// <summary>
        /// Gets all unique location types.
        /// </summary>
        /// <returns>All unique location types.</returns>
        [HttpGet("locationTypes")]
        public async Task<IList<string>> GetLocationTypesAsync()
        {
            var result = await _lookupService.GetLocationTypesAsync();
            return result;
        }

        /// <summary>
        /// Gets the Campus lookups.
        /// </summary>
        /// <returns>
        /// The Campus lookups.
        /// </returns>
        [HttpGet("campuses")]
        public async Task<IList<LocationLookupModel>> GetAllCampusesAsync()
        {
            var result = await _lookupService.GetAllCampusesAsync(); 
            return result;
        }

        /// <summary>
        /// Gets the region lookups of the Campus by Id.
        /// </summary>
        /// <param name="campusId">The campus Id.</param>
        /// <returns>
        /// The region lookups.
        /// </returns>
        [HttpGet("regions")]
        public async Task<IList<LookupModel>> GetCampusRegionLookupsAsync(string campusId)
        {
            var result = await _lookupService.GetCampusRegionLookupsAsync(campusId);
            return result;
        }

        /// <summary>
        /// Gets the state lookups.
        /// </summary>
        /// <returns>
        /// The state lookups.
        /// </returns>
        [HttpGet("states")]
        public async Task<IList<LookupModel>> GetStatesAsync()
        {
            var result = await _lookupService.GetStatesAsync();
            return result;
        }

        /// <summary>
        /// Gets the countries with corresponding states.
        /// </summary>
        /// <returns>
        /// The countries with corresponding states.
        /// </returns>
        [HttpGet("countryStates")]
        public async Task<IList<CountryStatesModel>> GetCountryStatesAsync()
        {
            var result = await _lookupService.GetCountryStatesAsync();
            return result;
        }

        /// <summary>
        /// Gets the child location lookups of the Region by Id.
        /// </summary>
        /// <param name="regionNodeId">The region node identifier.</param>
        /// <returns>
        /// The child location lookups.
        /// </returns>
        [HttpGet("childLocs")]
        public async Task<IList<ChildLocationLookupModel>> GetChildLocationLookupsAsync(string regionNodeId)
        {
            var result = await _lookupService.GetChildLocationLookupsAsync(regionNodeId);
            return result;
        }
    }
}
