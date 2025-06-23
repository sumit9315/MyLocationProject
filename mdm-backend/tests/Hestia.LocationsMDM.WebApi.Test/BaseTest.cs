using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Controllers;
using Hestia.LocationsMDM.WebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using Hestia.LocationsMDM.WebApi.Services.Impl;
using Microsoft.Azure.Cosmos;
using Hestia.LocationsMDM.WebApi.Models;
using System;

namespace Hestia.LocationsMDM.WebApi.Test
{
    public abstract partial class BaseTest<TTarget>
    {
        private const string TestResultsPath = @"../../../TestJsonResults/";

        protected const string DefaultAdminEmail = "admin@example.com";

        /// <summary>
        /// Represents the JSON serializer settings.
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatString = "MM/dd/yyyy HH:mm:ss",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Formatting = Formatting.Indented
        };

        protected ServiceProvider _serviceProvider;

        protected TTarget _target;

        protected IConfiguration Configuration { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            var services = new ServiceCollection();

            Configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
               .Build();
            services.AddSingleton(Configuration);

            // IHttpContextAccessor is not registered by default
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // configure DB connection
            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            // configure controllers
            services.AddScoped<UserController>();
            services.AddScoped<AuthController>();
            services.AddScoped<DashboardController>();
            services.AddScoped<HierarchyController>();
            services.AddScoped<CampusController>();
            services.AddScoped<ChildLocationController>();

            // configure repositories
            services.AddScoped<IHierarchyService, HierarchyService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IChildLocationService, ChildLocationService>();

            services.AddSingleton<CosmosClient>(x => new CosmosClient(Configuration.GetConnectionString("Cosmos")));

            services.Configure<CosmosConfig>(Configuration.GetSection("Cosmos"));

            _serviceProvider = services.BuildServiceProvider();

            _target = _serviceProvider.GetService<TTarget>();

            // avoid current user for services just add for controller test
            if (!_target.GetType().Name.Contains("Service"))
            {
                var prop = typeof(BaseController).GetField("_currentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop.SetValue(_target, new LoggedInUser
                {
                    Username = DefaultAdminEmail,
                    Role = "Admin"
                });
            }

            if (_target.GetType().Name.Contains("Controller"))
            {
                TestDataManager.SetUp(Configuration);
            }
        }

        protected T Resolve<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Compares actual test result with the expected result in JSON format.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="result">The result.</param>
        /// <param name="testName">Name of the test.</param>
        protected void AssertResult<T>(T result, [CallerMemberName] string testName = null)
        {
            bool develop = true;
            if (develop)
            {
                if (!Directory.Exists(TestResultsPath))
                {
                    Directory.CreateDirectory(TestResultsPath);
                }

                string jsonResult = JsonConvert.SerializeObject(result, SerializerSettings);

                string filePath = Path.Combine(TestResultsPath, $"{GetType().Name}.{testName}.json");
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, jsonResult);
                }
                else
                {
                    string existing = File.ReadAllText(filePath);
                    if (jsonResult != existing)
                    {
                        File.WriteAllText(filePath, jsonResult);
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(TestResultsPath, $"{GetType().Name}.{testName}.json");
                string expected = File.ReadAllText(filePath);
                string actual = JsonConvert.SerializeObject(result, SerializerSettings);
                Assert.AreEqual(expected, actual, "Mismatch in actual and expected result when serialized to JSON.");
            }
        }

        protected static NodeAddress CreateTestNodeAddress(int seed)
        {
            return new NodeAddress
            {
                CountryName = "country name " + seed,
                State = "state " + seed,
                CityName = "city name " + seed,
                PostalCodeExtn = "987" + seed,
                PostalCodePrimary = "1234" + seed,
                AddressLine1 = "address line 1 " + seed,
                AddressLine2 = "address line 2 " + seed,
                AddressLine3 = "address line 3 " + seed,
                AltShiptoCountryName = "Alt. Ship-To country name " + seed,
                AltShiptoState = "Alt. Ship-To state " + seed,
                AltShiptoCityName = "Alt. Ship-To city name " + seed,
                AltShiptoPostalCodePrimary = "2345" + seed,
                AltShiptoPostalCodeExtn = "876" + seed,
                AltShiptoAddressLine1 = "Alt. Ship-To address line 1 " + seed,
                AltShiptoAddressLine2 = "Alt. Ship-To address line 2 " + seed,
                AltShiptoAddressLine3 = "Alt. Ship-To address line 3 " + seed
            };
        }

        protected static OperatingHoursModel CreateOperatingHoursModel(int seed)
        {
            return new OperatingHoursModel
            {
                OperatingHoursId = "0" + seed,
                DayOfWeek = seed,
                OperatingHours = new TimeIntervalString
                {
                    StartTime = "06:0" + seed,
                    EndTime = "16:0" + seed
                },
                AfterHours = seed % 3 == 0 ? "Yes" : "No",
                ReceivingHours = $"06:0{seed}-15:0{seed}",
                EffectiveDate = new DateTime(2019, seed, 10 + seed),
                EndDate = new DateTime(2019 + seed, 12, 31),
                IsDaylightSaving = seed % 2 == 0
            };
        }
    }
}
