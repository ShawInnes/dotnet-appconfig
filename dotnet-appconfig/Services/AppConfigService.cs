using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using ConfigManager.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigManager.Services
{
    public class AppConfigService : IAppConfigService
    {
        private readonly IConsole _console;
        private const string KeyVaultReferenceContentType = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8";

        public bool ConsoleOutput { get; set; }

        public string FromKeyVaultReference(string value)
        {
            var regex = new Regex("/secrets/(?<secretname>[a-z0-9\\-]*)\"");
            var match = regex.Match(value);
            var secretName = match.Groups["secretname"].Value;
            return secretName;
        }

        public bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput))
            {
                return false;
            }

            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) // For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        public string ToKeyVaultReference(string keyVaultName, string secretName)
        {
            return $"{{\"uri\": \"https://{keyVaultName}.vault.azure.net/secrets/{secretName}\"}}";
        }

        public bool IsKeyVaultSetting(ConfigurationSetting setting)
        {
            return !string.IsNullOrEmpty(setting.ContentType) && setting.ContentType == KeyVaultReferenceContentType;
        }

        public (string, string, string, string) SplitAppConfigConnectionString(string connectionString)
        {
            var regex = new Regex("Endpoint=(?<endpoint>https://(?<name>.*).azconfig.io);Id=(?<id>.*);Secret=(?<secret>.*)");
            var match = regex.Match(connectionString);
            if (match.Success)
                return (match.Groups["endpoint"].Value, match.Groups["name"].Value, match.Groups["id"].Value, match.Groups["secret"].Value);

            throw new InvalidOperationException("Unable to parse AppConfig Connection String");
        }

        public AppConfigService(IConsole console)
        {
            _console = console;
        }

        public async Task ExportAppConfigurationToFileByName(string appConfigName, string outputPath)
        {
            if (ConsoleOutput) _console.WriteLine($"Exporting AppSettings and KeyVault References from '{appConfigName}' to {outputPath}");

            var json = await ExportAppConfigurationByName(appConfigName);

            await File.WriteAllTextAsync(outputPath, json);

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public async Task ExportAppConfigurationToFileByConnectionString(string connectionString, string outputPath)
        {
            var (_, name, _, _) = SplitAppConfigConnectionString(connectionString);
            if (ConsoleOutput) _console.WriteLine($"Exporting AppSettings and KeyVault References from '{name}' to {outputPath}");

            var json = await ExportAppConfigurationByConnectionString(connectionString);

            await File.WriteAllTextAsync(outputPath, json);

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public async Task ImportAppConfigurationFromFileByName(string appConfigName, string keyVaultName, string inputPath, bool dryRun)
        {
            var json = await File.ReadAllTextAsync(inputPath);
            if (!IsValidJson(json))
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"File '{inputPath}' does not appear to be valid Json");
            }
            else
            {
                if (ConsoleOutput) _console.WriteLine($"Importing AppSettings and KeyVault References from '{inputPath}' to '{appConfigName}' and '{keyVaultName}'");

                var configItems = JsonConvert.DeserializeObject<List<AppConfigItem>>(json);
                if (ConsoleOutput) _console.WriteLine($"Importing {configItems.Count} Item(s) into Azure App Config");
                await ImportAppConfigurationByName(appConfigName, keyVaultName, configItems, dryRun);
            }

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public async Task ImportAppConfigurationFromFileByConnectionString(string connectionString, string keyVaultName, string inputPath, bool dryRun)
        {
            var json = await File.ReadAllTextAsync(inputPath);
            if (!IsValidJson(json))
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"File '{inputPath}' does not appear to be valid Json");
            }
            else
            {
                var (_, name, _, _) = SplitAppConfigConnectionString(connectionString);

                if (ConsoleOutput) _console.WriteLine($"Importing AppSettings and KeyVault References from '{inputPath}' to '{name}' and '{keyVaultName}'");

                var configItems = JsonConvert.DeserializeObject<List<AppConfigItem>>(json);
                if (ConsoleOutput) _console.WriteLine($"Importing {configItems.Count} Item(s) into Azure App Config");
                await ImportAppConfigurationByConnectionString(keyVaultName, connectionString, configItems, dryRun);
            }

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public Task ImportAppConfigurationByName(string appConfigName, string keyVaultName, List<AppConfigItem> configItems, bool dryRun)
        {
            var configurationClient = GetConfigurationClientByName(appConfigName);
            return ImportAppConfiguration(configurationClient, keyVaultName, configItems, dryRun);
        }

        public Task ImportAppConfigurationByConnectionString(string keyVaultName, string appConfigConnectionString, List<AppConfigItem> configItems, bool dryRun)
        {
            var configurationClient = GetConfigurationClientByConnectionString(appConfigConnectionString);
            return ImportAppConfiguration(configurationClient, keyVaultName, configItems, dryRun);
        }

        private static ConfigurationClient GetConfigurationClientByConnectionString(string appConfigConnectionString)
        {
            var configurationClient = new ConfigurationClient(appConfigConnectionString);
            return configurationClient;
        }

        private static ConfigurationClient GetConfigurationClientByName(string appConfigName)
        {
            var configurationClient = new ConfigurationClient(new Uri($"https://{appConfigName}.azconfig.io"), new DefaultAzureCredential());
            return configurationClient;
        }

        private Task ImportAppConfiguration(ConfigurationClient configurationClient, string keyVaultName, List<AppConfigItem> configItems, bool dryRun)
        {
            try
            {
                var configurationSettings = configurationClient.GetConfigurationSettings(new SettingSelector()).ToList();
                var dryRunPrefix = dryRun ? "[dry-run] " : "";

                foreach (var appConfigItem in configItems)
                {
                    if (appConfigItem.Purge)
                    {
                        if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.Red;
                        if (ConsoleOutput) _console.WriteLine($"{dryRunPrefix}Purging AppConfiguration Item '{appConfigItem.Key}'");

                        if (!dryRun) configurationClient.DeleteConfigurationSetting(appConfigItem.Key);
                    }
                    else
                    {
                        var configurationSetting = configurationSettings.FirstOrDefault(p => p.Key == appConfigItem.Key);

                        if (configurationSetting == null)
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGreen;
                            if (ConsoleOutput) _console.WriteLine($"{dryRunPrefix}Adding new AppConfiguration {(appConfigItem.KeyVault ? "KeyVault Reference" : "Item")} '{appConfigItem.Key}'");

                            configurationSetting = new ConfigurationSetting(appConfigItem.Key, appConfigItem.KeyVault ? ToKeyVaultReference(keyVaultName, appConfigItem.Value) : appConfigItem.Value);
                            configurationSetting.ContentType = appConfigItem.KeyVault ? KeyVaultReferenceContentType : null;

                            if (!dryRun) configurationClient.AddConfigurationSetting(configurationSetting);
                        }
                        else if ((!appConfigItem.KeyVault && appConfigItem.Value != configurationSetting.Value) || appConfigItem.KeyVault && ToKeyVaultReference(keyVaultName, appConfigItem.Value) != configurationSetting.Value)
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkYellow;
                            if (ConsoleOutput) _console.WriteLine($"{dryRunPrefix}Updating Value of AppConfiguration Item '{appConfigItem.Key}'");

                            configurationSetting.Key = appConfigItem.Key;
                            configurationSetting.Value = appConfigItem.KeyVault ? ToKeyVaultReference(keyVaultName, appConfigItem.Value) : appConfigItem.Value;
                            configurationSetting.ContentType = appConfigItem.KeyVault ? KeyVaultReferenceContentType : null;

                            if (!dryRun) configurationClient.SetConfigurationSetting(configurationSetting);
                        }
                        else
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGray;
                            if (ConsoleOutput) _console.WriteLine($"{dryRunPrefix}Not Updating AppConfiguration Item '{appConfigItem.Key}'");
                        }
                    }
                }

                return Task.CompletedTask;
            }
            catch (AuthenticationFailedException ex)
            {
                _console.ForegroundColor = ConsoleColor.DarkRed;
                _console.WriteLine($"Error Authenticating to App Config or KeyVault: \n {ex.Message}");
                throw;
            }
        }

        public async Task<string> ExportAppConfigurationByName(string appConfigName)
        {
            var configurationClient = GetConfigurationClientByName(appConfigName);

            return await ExportAppConfiguration(configurationClient);
        }

        public async Task<string> ExportAppConfigurationByConnectionString(string appConfigConnectionString)
        {
            var configurationClient = GetConfigurationClientByConnectionString(appConfigConnectionString);

            return await ExportAppConfiguration(configurationClient);
        }

        private async Task<string> ExportAppConfiguration(ConfigurationClient configurationClient)
        {
            List<AppConfigItem> configItems = new List<AppConfigItem>();

            try
            {
                var configurationSettings = configurationClient.GetConfigurationSettings(new SettingSelector());
                foreach (var configurationSetting in configurationSettings)
                {
                    var appConfigItem = new AppConfigItem
                    {
                        Key = configurationSetting.Key,
                        Label = configurationSetting.Label,
                        Value = configurationSetting.Value,
                    };

                    if (IsKeyVaultSetting(configurationSetting))
                    {
                        appConfigItem.Value = FromKeyVaultReference(configurationSetting.Value);
                        appConfigItem.KeyVault = true;

                        if (ConsoleOutput) _console.WriteLine($"Exporting AppConfiguration KeyVault Reference '{appConfigItem.Key}'");
                    }
                    else
                    {
                        if (ConsoleOutput) _console.WriteLine($"Exporting AppConfiguration Item '{appConfigItem.Key}'");
                    }

                    configItems.Add(appConfigItem);
                }

                return await Task.FromResult(JsonConvert.SerializeObject(configItems.OrderBy(p => p.Key), new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                }));
            }
            catch (AuthenticationFailedException ex)
            {
                _console.ForegroundColor = ConsoleColor.DarkRed;
                _console.WriteLine($"Error Authenticating to App Config or KeyVault: \n {ex.Message}");
                throw;
            }
        }
    }
}
