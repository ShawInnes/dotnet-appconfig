using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
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

        public async Task ExportAppConfigurationToFile(string connectionString, string outputPath)
        {
            var (_, name, _, _) = SplitAppConfigConnectionString(connectionString);

            if (ConsoleOutput) _console.WriteLine($"Exporting AppSettings and KeyVault References from '{name}' to {outputPath}");

            var json = await ExtractAppConfiguration(connectionString);

            await File.WriteAllTextAsync(outputPath, json);

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public async Task ImportAppConfigurationFromFile(string connectionString, string keyVaultName, string inputPath, bool dryRun)
        {
            var (_, name, _, _) = SplitAppConfigConnectionString(connectionString);

            if (ConsoleOutput) _console.WriteLine($"Importing AppSettings and KeyVault References from '{inputPath}' to '{name}' and '{keyVaultName}'");

            var json = await File.ReadAllTextAsync(inputPath);
            if (!IsValidJson(json))
            {
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine($"File '{inputPath}' does not appear to be valid Json");
            }
            else
            {
                var configItems = JsonConvert.DeserializeObject<List<AppConfigItem>>(json);
                if (ConsoleOutput) _console.WriteLine($"Importing {configItems.Count} Item(s) into Azure App Config");
                await UpdateAppConfiguration(keyVaultName, connectionString, configItems, dryRun);
            }

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }


        public Task UpdateAppConfiguration(string keyVaultName, string appConfigConnectionString, List<AppConfigItem> configItems, bool dryRun)
        {
            var configurationClient = new ConfigurationClient(appConfigConnectionString);
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
                        if (ConsoleOutput) _console.WriteLine($"{dryRunPrefix}Adding new AppConfiguration Item '{appConfigItem.Key}'");

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

        public async Task<string> ExtractAppConfiguration(string appConfigConnectionString)
        {
            var configurationClient = new ConfigurationClient(appConfigConnectionString);

            List<AppConfigItem> configItems = new List<AppConfigItem>();

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
    }
}
