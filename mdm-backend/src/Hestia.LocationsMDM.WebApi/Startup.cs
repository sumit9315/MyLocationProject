using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Hestia.LocationsMDM.WebApi.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hestia.LocationsMDM.WebApi.Services;
using Hestia.LocationsMDM.WebApi.Services.Impl;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Hestia.LocationsMDM.WebApi.Extensions.Swagger;
using Azure.Identity;
using Microsoft.Azure.Cosmos.Fluent;
using System;

namespace Hestia.LocationsMDM.WebApi
{
    /// <summary>
    /// The startup class for the application.
    /// </summary>
    public class Startup
    {
        private const string JsonContentType = "application/json";

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // configure services
            services.AddScoped<IAuthorizationHandler, CheckADGroupHandler>();
            services.AddScoped<IHierarchyService, HierarchyService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IChildLocationService, ChildLocationService>();
            services.AddScoped<IAssociateService, AssociateService>();
            services.AddScoped<IRegionService, RegionService>();
            services.AddScoped<ICalendarEventService, CalendarEventService>();
            services.AddScoped<IChangeHistoryService, ChangeHistoryService>();
            services.AddScoped<ILovService, LovService>();
            services.AddScoped<ILookupService, LookupService>();
            services.AddScoped<IPricingRegionService, PricingRegionService>();

            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
            services.AddCors();

            // IHttpContextAccessor is not registered by default
            //services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpContextAccessor();
            services.AddScoped<IAppContextProvider, AppContextProvider>();
            services.AddSingleton<IAuthenticationSchemeProvider, CustomAuthenticationSchemeProvider>();

            // adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddMicrosoftIdentityWebApiAuthentication(_configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddMicrosoftGraph(_configuration.GetSection("GraphAPI"))
                .AddInMemoryTokenCaches();

            //services.AddJwtBearerAuthentication(_configuration);

            services.AddCors();
            services.AddLogging();
            services.AddControllers(options =>
            {
                 var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = false;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ValidateAccessTokenPolicy", validateAccessTokenPolicy =>
                {
                    // Validate ClientId from token
                    // only accept tokens issued ....
                    validateAccessTokenPolicy.RequireClaim("azp", _configuration["AzureAD:ClientId"]);
                });
                options.AddPolicy("AdminOnly", policy =>
                {
                    var groups = new List<string>
                    {
                        _configuration["SecuritySettings:AdminGroup"]
                    };
                    policy.Requirements.Add(new CheckADGroupRequirement(groups));
                });
                options.AddPolicy("User", policy =>
                {
                    var groups = new List<string>
                    {
                        _configuration["SecuritySettings:AdminGroup"],
                        _configuration["SecuritySettings:UserGroup"]
                    };
                    policy.Requirements.Add(new CheckADGroupRequirement(groups));
                });
            });

            services.AddMemoryCache();

            string dbEndpoint = _configuration.GetConnectionString("CosmosDbEndpoint");
            if (!string.IsNullOrEmpty(dbEndpoint))
            {
                var credential = new DefaultAzureCredential();

                services.AddSingleton(x => new CosmosClientBuilder(dbEndpoint, credential)
                    .WithSerializerOptions(new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase })
                    .Build());
            }
            else
            {
                string connString = _configuration.GetConnectionString("Cosmos");
                services.AddSingleton<CosmosClient>(x => new CosmosClient(connString));
            }

            // Add Swagger Gen
            services.AddSwaggerGeneration(_configuration);

            services.Configure<CosmosConfig>(_configuration.GetSection("Cosmos"));
            services.Configure<SecurityConfig>(_configuration.GetSection("SecuritySettings"));
            services.Configure<GraphApiConfig>(_configuration.GetSection("GraphAPI"));
            services.AddApplicationInsightsTelemetry(_configuration.GetSection("ApplicationInsights").GetValue<string>("InstrumentationKey"));
            //services.AddApplicationInsightsKubernetesEnricher();
            services.AddHealthChecks();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        /// <param name="logger">The logger.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.ConfigureExceptionHandler(logger);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
            });

            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.ConfigureSwagger(_configuration);
        }
    }
}
