using Microsoft.Extensions.Configuration;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Hestia.LocationsMDM.WebApi.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The base controller.
    /// </summary>
    [Authorize(Policy = "ValidateAccessTokenPolicy", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(Policy = "User")]
    [ApiController]
    [Route("{controller}")]
    public abstract class BaseController : ControllerBase
    {
        protected const string BaseHierarchyTopLevelCacheKey = "BaseHierarchy_TopLevel";
        protected const string PhysicalHierarchyTopLevelCacheKey = "PhysicalHierarchy_TopLevel";
        protected const string ManagementHierarchyTopLevelCacheKey = "ManagementHierarchy_TopLevel";

        /// <summary>
        /// The configuration
        /// </summary>
        protected readonly IConfiguration _config;

        /// <summary>
        /// The memory cache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// The current user
        /// </summary>
        private LoggedInUser _currentUser;

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        protected LoggedInUser CurrentUser
        {
            get
            {
                if (_currentUser == null)
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        _currentUser = User.ToLoggedInUser();
                    }
                }
                return _currentUser;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        protected BaseController()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="memoryCache">The memory cache.</param>
        protected BaseController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        protected BaseController(IConfiguration configuration)
        {
            _config = configuration;
        }

        /// <summary>
        /// Validates the create events model.
        /// </summary>
        /// <param name="model">The model.</param>
        protected static void ValidateEventsModel(LocationEventsModel model)
        {
            Util.ValidateArgumentNotNull(model, nameof(model));
            Util.ValidateArgumentNotNull(model.PlannedEvents, nameof(model.PlannedEvents));
            Util.ValidateArgumentNotNull(model.UnplannedEvents, nameof(model.UnplannedEvents));

            // make sure event type is properly set
            model.PlannedEvents.ForEach(x => x.EventType = CalendarEventType.Planned);
            model.UnplannedEvents.ForEach(x => x.EventType = CalendarEventType.Unplanned);
        }

        /// <summary>
        /// Updates the campus name in base hierarchy cache.
        /// </summary>
        /// <param name="campusId">The campus node Id.</param>
        /// <param name="locationName">Updated campus name.</param>
        protected void UpdateCampusNameInBaseHierarchyCache(string campusId, string locationName)
        {
            if (_memoryCache == null)
            {
                throw new ServiceException($"{typeof(IMemoryCache)} must be injected in the constructor.");
            }

            // try get data from cache
            if (_memoryCache.TryGetValue(BaseHierarchyTopLevelCacheKey, out IList<HierarchyNode> items))
            {
                // find and update Campus name
                var campus = items.FirstOrDefault(x => x.Id == campusId);
                campus.Name = locationName;
            }
        }
    }
}
