using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Common
{
    public class CheckADGroupRequirement : IAuthorizationRequirement
    {
        public IList<string> Groups { get; private set; }

        public CheckADGroupRequirement(IList<string> groups)
        {
            Groups = groups;
        }
    }

}
