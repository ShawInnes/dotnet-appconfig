using System;
using Azure.Data.AppConfiguration;
using Azure.Identity;

namespace ConfigManager.Services
{
    public class ConfigurationClientHelpers
    {
        public static ConfigurationClient GetConfigurationClientByConnectionString(string appConfigConnectionString)
        {
            var configurationClient = new ConfigurationClient(appConfigConnectionString);
            return configurationClient;
        }

        public static ConfigurationClient GetConfigurationClientByName(string appConfigName)
        {
            var configurationClient = new ConfigurationClient(new Uri($"https://{appConfigName}.azconfig.io"),
                new DefaultAzureCredential(), new ConfigurationClientOptions()
                {
                    Diagnostics =
                    {
                        IsLoggingEnabled = true,
                        IsLoggingContentEnabled = true,
                    }
                });
            return configurationClient;
        }
    }
}