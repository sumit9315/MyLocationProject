using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Hestia.LocationsMDM.WebApi.Extensions
{
    public class PrefixKeyVaultSecretManager : KeyVaultSecretManager
    {
        private readonly string _prefix;

        public PrefixKeyVaultSecretManager(string prefix)
        {
            _prefix = prefix;
        }

        public override bool Load(SecretProperties secret)
        {
            return secret.Name.StartsWith(_prefix);
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            string normalizedName = secret.Name;
            if (normalizedName.StartsWith(_prefix))
            {
                normalizedName = normalizedName.Substring(_prefix.Length);
            }

            normalizedName = normalizedName.Replace("--", ConfigurationPath.KeyDelimiter);
            return normalizedName;
        }
    }
}
