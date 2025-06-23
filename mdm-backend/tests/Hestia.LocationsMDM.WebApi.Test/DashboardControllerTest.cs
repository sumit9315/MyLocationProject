using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hestia.LocationsMDM.WebApi.Controllers;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class DashboardControllerTest : BaseTest<DashboardController>
    {
        [TestMethod]
        public void TestGetStatistics()
        {
            var result = _target.GetStatisticsAsync().Result;
            AssertResult(result);
        }
    }
}
