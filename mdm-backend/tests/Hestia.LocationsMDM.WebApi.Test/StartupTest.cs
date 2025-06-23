using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hestia.LocationsMDM.WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hestia.LocationsMDM.WebApi.Test
{
    [TestClass]
    public class StartupTest : BaseTest<AuthController>
    {
        [TestMethod]
        public void TestCtor1()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
               .Build();

            var startup = new Startup(configuration);
            startup.ConfigureServices(services);
        }
    }
}
