using Hestia.LocationsMDM.WebApi.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class AccountControllerTest : BaseTest<UserController>
    {
        [TestMethod]
        public void TestGetProfile()
        {
            var result = _target.GetMe();
            AssertResult(result);
        }
    }
}
