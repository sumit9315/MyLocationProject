using Hestia.LocationsMDM.WebApi.DTOs;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The application context provider interface.
    /// </summary>
    public interface IAppContextProvider
    {
        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <returns>The current user details.</returns>
        UserInfo GetCurrentUser();

        /// <summary>
        /// Gets the full name of the current user.
        /// </summary>
        /// <returns>The full name.</returns>
        string GetCurrentUserFullName();
    }
}