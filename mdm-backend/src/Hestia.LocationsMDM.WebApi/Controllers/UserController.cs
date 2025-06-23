using Microsoft.AspNetCore.Mvc;
using Hestia.LocationsMDM.WebApi.Common;

namespace Hestia.LocationsMDM.WebApi.Controllers
{
    /// <summary>
    /// The User controller.
    /// </summary>
    [Route("")]
    public class UserController : BaseController
    {
        /// <summary>
        /// Gets current user info.
        /// </summary>
        /// <returns>The user info.</returns>
        [HttpGet("me")]
        public LoggedInUser GetMe()
        {
            return CurrentUser;
        }
    }
}
