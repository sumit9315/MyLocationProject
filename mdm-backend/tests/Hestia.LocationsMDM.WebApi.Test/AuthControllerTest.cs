using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Hestia.LocationsMDM.WebApi.Controllers;
using Hestia.LocationsMDM.WebApi.DTOs;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class AuthControllerTest : BaseTest<AuthController>
    {
        [TestMethod]
        public void TestLogin()
        {
            var result = _target.Login(new LoginRequestDto
            {
                Username = "admin"
            });

            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
            var resultToAssert = new
            {
                token.Issuer,
                token.Audiences,
                Claims = token.Claims.Where(x => x.Type != JwtRegisteredClaimNames.Exp && x.Type != JwtRegisteredClaimNames.Jti).ToList()
            };

            AssertResult(resultToAssert);
        }
    }
}
