using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// This controller exposes security related operations.
    /// </summary>
    public class AuthController : BaseController
    {
        /// <summary>
        /// Gets currently logged in user details.
        /// </summary>
        /// <returns>The JWT token.</returns>
        [HttpGet("me")]
        public LoggedInUser GetUserDetails()
        {
            return CurrentUser;
        }
    }
}
