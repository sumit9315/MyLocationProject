using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hestia.LocationsMDM.WebApi.Common
{
    public class CheckADGroupHandler : AuthorizationHandler<CheckADGroupRequirement>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IOptions<GraphApiConfig> _optionsGraph;
        private readonly IOptions<SecurityConfig> _optionsSec;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckADGroupHandler> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenAcquisition">The acquisition token service.</param>
        /// <param name="memoryCache">The memory cache service.</param>
        /// <param name="clientFactory">The http client service.</param>
        /// <param name="optionsGraph">The Graph API options.</param>
        /// <param name="optionsSec">The AD security options.</param>
        public CheckADGroupHandler(ITokenAcquisition tokenAcquisition,
            IMemoryCache memoryCache,
            IHttpClientFactory clientFactory,
            IOptions<GraphApiConfig> optionsGraph,
            IOptions<SecurityConfig> optionsSec,
            IConfiguration configuration,
            ILogger<CheckADGroupHandler> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _memoryCache = memoryCache;
            _clientFactory = clientFactory;
            _optionsGraph = optionsGraph;
            _optionsSec = optionsSec;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Makes a decision if authorization is allowed based on a specific requirement.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The requirement to evaluate.</param>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CheckADGroupRequirement requirement)
        {
            _logger.LogInformation("In HandleRequirementAsync(...)");

            string keyVaultUri = _configuration["KeyVaultURI"];
            _logger.LogInformation($"Config - Key Vault URI: {keyVaultUri}");

            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            if (clientSecret?.Length > 1)
            {
                clientSecret = $"{clientSecret[0]}...{clientSecret[^1]}";
            }
            _logger.LogInformation($"Config AzureAd ClientId/ClientSecret: {clientId}/{clientSecret}");

            var user = context.User.ToLoggedInUser();
            var result = _optionsGraph.Value.UseGraphApi
                ? await CheckMemberGroupsAPI(user, requirement.Groups)
                : CheckMemberGroupsToken(user, requirement.Groups);
            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        #region private methods

        /// <summary>
        /// Check member groups against Access Token.
        /// </summary>
        /// <param name="wi">The logged in user.</param>
        /// <param name="groupIds">The group ids to check against.</param>
        private bool CheckMemberGroupsToken(LoggedInUser wi, IList<string> groupIds)
        {
            if (wi.Roles == null)
            {
                return false;
            }

            try
            {
                var results = groupIds.Intersect(wi.Roles);
                wi.Role = GetUserRole(results.ToList());
                return results.Count() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check member groups against Graph API.
        /// </summary>
        /// <param name="wi">The logged in user.</param>
        /// <param name="groupIds">The group ids to check against.</param>
        private async Task<bool> CheckMemberGroupsAPI(LoggedInUser wi, IList<string> groupIds)
        {
            try
            {
                string cacheId = string.Join("-", groupIds) + wi.Id;
                if (!_memoryCache.TryGetValue(cacheId, out List<string> cachedResults))
                {
                    var scopes = _optionsGraph.Value.Scopes.Split(",");
                    var graphServiceClient = await GetGraphClient(scopes);
                    var results = await graphServiceClient
                        .Me
                        .CheckMemberGroups(groupIds)
                        .Request()
                        .PostAsync()
                        .ConfigureAwait(false);
                    if (results.Count > 0)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                        {
                            AbsoluteExpiration = DateTime.Now.AddSeconds(_optionsGraph.Value.CacheExpiration)
                        };
                        _memoryCache.Set(cacheId, results.ToList(), cacheEntryOptions);
                        wi.Role = GetUserRole(results.ToList());
                        return true;
                    }
                }
                else if (cachedResults.Count > 0)
                {
                    wi.Role = GetUserRole(cachedResults);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get user role from groups.
        /// </summary>
        /// <param name="groups">The list of groups from member.</param>
        private string GetUserRole(IList<string> groups)
        {
            if (groups.Contains(_optionsSec.Value.AdminGroup))
            {
                return "admin";
            }
            else if (groups.Contains(_optionsSec.Value.UserGroup))
            {
                return "user";
            }
            throw new Exception();
        }

        /// <summary>
        /// Get Graph API service client.
        /// </summary>
        /// <param name="scopes">The API scopes.</param>
        private async Task<GraphServiceClient> GetGraphClient(string[] scopes)
        {
            var token = await _tokenAcquisition
                .GetAccessTokenForUserAsync(scopes)
                .ConfigureAwait(false);

            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(_optionsGraph.Value.BaseUrl);
            client
                .DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            GraphServiceClient graphClient = new GraphServiceClient(client)
            {
                AuthenticationProvider = new DelegateAuthenticationProvider(
                (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                    return Task.CompletedTask;
                })
            };

            graphClient.BaseUrl = client.BaseAddress.ToString();
            return graphClient;
        }

        #endregion
    }
}