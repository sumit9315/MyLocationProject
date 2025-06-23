using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Hestia.LocationsMDM.WebApi.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Hestia.LocationsMDM.WebApi
{
    /// <summary>
    /// Entry point of the application.
    /// </summary>
    public class Program
    {
        private const string AzureKeyVaultSecretPrefix = "fei-mdmlocation-BackendApi-";

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates the host builder.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The host builder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // add config values from Azure Key Vault
                var builtConfig = config.Build();
                var keyVaultUri = builtConfig["KeyVaultURI"];
                Console.WriteLine($"KEY Vault URI: {keyVaultUri}");

                try
                {
                    if (!string.IsNullOrEmpty(keyVaultUri))
                    {
                        var credential = new DefaultAzureCredential();

                        var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

                        var secretKey = $"{AzureKeyVaultSecretPrefix}AzureAd--ClientSecret";
                        var secret = secretClient.GetSecret(secretKey);

                        config.AddAzureKeyVault(secretClient, new PrefixKeyVaultSecretManager(AzureKeyVaultSecretPrefix));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            });
            
            builder.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
            return builder;
        }
    }
}
