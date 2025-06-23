using Hestia.LocationsMDM.WebApi.DTOs;
using Microsoft.AspNetCore.Http;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The application context provider.
    /// </summary>
    public class AppContextProvider : IAppContextProvider
    {
        /// <summary>
        /// The name claim type.
        /// </summary>
        private const string NameClaimType = "name";

        /// <summary>
        /// The HTTP context accessor.
        /// </summary>
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppContextProvider"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public AppContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <returns>The current user details.</returns>
        public UserInfo GetCurrentUser()
        {
            if (_httpContextAccessor.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var name = _httpContextAccessor.HttpContext.User.FindFirst(NameClaimType);
            return new UserInfo
            {
                FullName = name.Value
            };
        }

        /// <summary>
        /// Gets the full name of the current user.
        /// </summary>
        /// <returns>The full name.</returns>
        public string GetCurrentUserFullName()
        {
            return GetCurrentUser()?.FullName;
        }
    }
}
