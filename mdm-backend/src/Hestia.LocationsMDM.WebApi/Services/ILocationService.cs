using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The service interface for common Location operations.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Gets the calendar events of the given location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The node.</param>
        /// <returns>The calendar events.</returns>
        Task<LocationCalendarEventsModel> GetEventsAsync(NodeType nodeType, string node);

        /// <summary>
        /// Updates events for the given Location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="node">The Child Location Id.</param>
        /// <param name="model">The events details model.</param>
        /// <returns></returns>
        Task UpdateEventsAsync(NodeType nodeType, string node, LocationEventsModel model);
    }
}
