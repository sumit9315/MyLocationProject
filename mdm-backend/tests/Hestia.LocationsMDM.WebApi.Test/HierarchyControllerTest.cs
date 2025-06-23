using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hestia.LocationsMDM.WebApi.Controllers;
using Hestia.LocationsMDM.WebApi.Models;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class HierarchyControllerTest : BaseTest<HierarchyController>
    {
        [TestMethod]
        public void TestGetHierarchy1()
        {
            var result = _target.GetAsync(HierarchyType.Base).Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy2()
        {
            var result = _target.GetAsync(HierarchyType.Base, "100000006").Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy3()
        {
            var result = _target.GetAsync(HierarchyType.Physical).Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy4()
        {
            var result = _target.GetAsync(HierarchyType.Physical, "100000007").Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy5()
        {
            var result = _target.GetAsync(HierarchyType.Management).Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy6()
        {
            var result = _target.GetAsync(HierarchyType.Management, "100000007").Result;
            AssertResult(result);
        }

        [TestMethod]
        public void TestGetHierarchy7()
        {
            var result = _target.GetAsync(HierarchyType.Management, "100000008").Result;
            AssertResult(result);
        }
    }
}
