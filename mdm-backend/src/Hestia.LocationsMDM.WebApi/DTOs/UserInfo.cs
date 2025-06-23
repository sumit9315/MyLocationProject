using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.DTOs
{
    public class UserInfo
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string UserRole { get; set; }
    }
}
