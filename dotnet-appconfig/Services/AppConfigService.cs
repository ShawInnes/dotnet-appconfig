using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Models;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigManager.Services
{
    public class AppConfigService : IAppConfigService
    {
        private readonly IConsole _console;
        private readonly ConfigurationClient _configurationClient;
        private readonly SecretClient _secretClient;

        private const string KeyVaultReferenceContentType =
            "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8";

        public bool ConsoleOutput { get; set; }

        public AppConfigService(IConsole console, ConfigurationClient configurationClient, SecretClient secretClient)
        {
            _console = console;
            _configurationClient = configurationClient;
            _secretClient = secretClient;
        }

        public string FromKeyVaultReference(string value)
        {
            var regex = new Regex("/secrets/(?<secretname>[a-z0-9\\-]*)\"");
            var match = regex.Match(value);
            var secretName = match.Groups["secretname"].Value;
            return secretName;
        }

        public static bool IsValidJson(string strInput)
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
            var regex = new Regex(
                "Endpoint=(?<endpoint>https://(?<name>.*).azconfig.io);Id=(?<id>.*);Secret=(?<secret>.*)");
            var match = regex.Match(connectionString);
            if (match.Success)
                return (match.Groups["endpoint"].Value, match.Groups["name"].Value, match.Groups["id"].Value,
                    match.Groups["secret"].Value);

            throw new InvalidOperationException("Unable to parse AppConfig Connection String");
        }

        public async Task ExportAppConfigurationToFile(string connectionString, string outputPath)
        {
            var (_, name, _, _) = SplitAppConfigConnectionString(connectionString);
            if (ConsoleOutput)
                _console.WriteLine($"Exporting AppSettings and KeyVault References from '{name}' to {outputPath}");

            var json = await ExportAppConfiguration();

            await File.WriteAllTextAsync(outputPath, json);

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public async Task ImportAppConfigurationFromFile(string connectionString,
            string keyVaultName,
            string inputPath,
            bool dryRun,
            bool strict)
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

                if (ConsoleOutput)
                    _console.WriteLine(
                        $"Importing AppSettings and KeyVault References from '{inputPath}' to '{name}' and '{keyVaultName}'");

                var (configItems, errors) = ReadAppConfigItems(json);

                if (errors.Any())
                {
                    _console.ForegroundColor = ConsoleColor.Red;

                    foreach (var error in errors)
                        _console.WriteLine(error);

                    _console.ResetColor();

                    throw new InvalidOperationException("JSON Parsing Errors");
                }

                if (ConsoleOutput) _console.WriteLine($"Importing {configItems.Count} Item(s) into Azure App Config");

                await ImportAppConfiguration(keyVaultName, configItems, dryRun, strict);
            }

            if (ConsoleOutput) _console.ResetColor();
            if (ConsoleOutput) _console.WriteLine($"Done.");
        }

        public static string ConfigItemToLabel(AppConfigItem configItem)
        {
            if (!string.IsNullOrEmpty(configItem.Environment) && !string.IsNullOrEmpty(configItem.Application))
                return $"Environment:{configItem.Environment}/Application:{configItem.Application}";

            if (!string.IsNullOrEmpty(configItem.Environment))
                return $"Environment:{configItem.Environment}";

            if (!string.IsNullOrEmpty(configItem.Application))
                return $"Application:{configItem.Application}";

            return configItem.Label;
        }

        public static AppConfigItem LabelToConfigItem(AppConfigItem configItem, string label)
        {
            if (label == null || string.IsNullOrEmpty(label))
                return configItem;
            
            var regex = new Regex(
                "(Environment:(?<environment>[A-Za-z0-9\\-_]*))|(Application:(?<application>[A-Za-z0-9\\-_]*))|(?<label>[A-Za-z0-9\\-_]+)");
            var matches = regex.Matches(label);

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups["environment"].Success)
                {
                    configItem.Environment = match.Groups["environment"].Value;
                }

                if (match.Success && match.Groups["application"].Success)
                {
                    configItem.Application = match.Groups["application"].Value;
                }

                if (match.Success && match.Groups["label"].Success)
                {
                    configItem.Label = match.Groups["label"].Value;
                }
            }

            return configItem;
        }

        public static (List<AppConfigItem>, List<string>) ReadAppConfigItems(string json)
        {
            List<string> errors = new List<string>();

            var configItems = JsonConvert.DeserializeObject<List<AppConfigItem>>(json, new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    errors.Add(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });

            if (configItems != null)
            {
                var validator = new AppConfigItemListValidator();
                var validatorResult = validator.Validate(configItems);
                if (!validatorResult.IsValid)
                {
                    foreach (var error in validatorResult.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
            }

            return (configItems, errors);
        }

        private async Task ImportAppConfiguration(string keyVaultName,
            List<AppConfigItem> configItems,
            bool dryRun,
            bool strict)
        {
            try
            {
                var configurationSettings =
                    _configurationClient.GetConfigurationSettings(new SettingSelector()).ToList();
                var dryRunPrefix = dryRun ? "[dry-run] " : "";

                foreach (var appConfigItem in configItems)
                {
                    if (appConfigItem.Purge)
                    {
                        if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.Red;
                        if (ConsoleOutput)
                            _console.WriteLine($"{dryRunPrefix}Purging AppConfiguration Item '{appConfigItem.Key}'");

                        if (!dryRun) await _configurationClient.DeleteConfigurationSettingAsync(appConfigItem.Key);
                    }
                    else
                    {
                        var configurationSetting =
                            configurationSettings.FirstOrDefault(p =>
                                p.Key == appConfigItem.Key && p.Label == ConfigItemToLabel(appConfigItem));

                        if (strict && appConfigItem.KeyVault)
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGray;
                            if (ConsoleOutput)
                                _console.WriteLine($"{dryRunPrefix}Validating KeyVault Item for '{appConfigItem.Key}'");

                            if (!(await IsValidKeyVaultItem(appConfigItem.Value)))
                            {
                                if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkRed;
                                if (ConsoleOutput)
                                    _console.WriteLine(
                                        $"{dryRunPrefix}Missing KeyVault Item '{appConfigItem.Value}' for '{appConfigItem.Key}'");
                            }
                            else
                            {
                                if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGreen;
                                if (ConsoleOutput)
                                    _console.WriteLine(
                                        $"{dryRunPrefix}Found KeyVault Item '{appConfigItem.Value}' for '{appConfigItem.Key}'");
                            }
                        }

                        if (configurationSetting == null)
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGreen;
                            if (ConsoleOutput)
                                _console.WriteLine(
                                    $"{dryRunPrefix}Adding new AppConfiguration {(appConfigItem.KeyVault ? "KeyVault Reference" : "Item")} '{appConfigItem.Key}'");

                            configurationSetting = new ConfigurationSetting(appConfigItem.Key,
                                appConfigItem.KeyVault
                                    ? ToKeyVaultReference(keyVaultName, appConfigItem.Value)
                                    : appConfigItem.Value, ConfigItemToLabel(appConfigItem))
                            {
                                ContentType = appConfigItem.KeyVault ? KeyVaultReferenceContentType : null
                            };

                            if (!dryRun)
                                await _configurationClient.AddConfigurationSettingAsync(configurationSetting);
                        }
                        else if ((!appConfigItem.KeyVault && appConfigItem.Value != configurationSetting.Value) ||
                                 appConfigItem.KeyVault && ToKeyVaultReference(keyVaultName, appConfigItem.Value) !=
                                 configurationSetting.Value)
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkYellow;
                            if (ConsoleOutput)
                                _console.WriteLine(
                                    $"{dryRunPrefix}Updating Value of AppConfiguration Item '{appConfigItem.Key}'");


                            configurationSetting.Key = appConfigItem.Key;
                            configurationSetting.Value = appConfigItem.KeyVault
                                ? ToKeyVaultReference(keyVaultName, appConfigItem.Value)
                                : appConfigItem.Value;
                            configurationSetting.ContentType =
                                appConfigItem.KeyVault ? KeyVaultReferenceContentType : null;

                            if (!dryRun)
                                await _configurationClient.SetConfigurationSettingAsync(configurationSetting);
                        }
                        else
                        {
                            if (ConsoleOutput) _console.ForegroundColor = ConsoleColor.DarkGray;
                            if (ConsoleOutput)
                                _console.WriteLine(
                                    $"{dryRunPrefix}Not Updating AppConfiguration Item '{appConfigItem.Key}'");
                        }
                    }
                }
            }
            catch (AuthenticationFailedException ex)
            {
                _console.ForegroundColor = ConsoleColor.DarkRed;
                _console.WriteLine($"Error Authenticating to App Config or KeyVault: \n {ex.Message}");
                throw;
            }
        }

        private async Task<bool> IsValidKeyVaultItem(string value)
        {
            var secret = await _secretClient.GetSecretAsync(value);
            if (secret != null)
                return await Task.FromResult(true);

            return await Task.FromResult(false);
        }

        public async Task<string> ExportAppConfiguration()
        {
            List<AppConfigItem> configItems = new List<AppConfigItem>();

            try
            {
                var configurationSettings = _configurationClient.GetConfigurationSettings(new SettingSelector());
                foreach (var configurationSetting in configurationSettings)
                {
                    var appConfigItem = new AppConfigItem
                    {
                        Key = configurationSetting.Key,
                        Value = configurationSetting.Value,
                    };

                    appConfigItem = LabelToConfigItem(appConfigItem, configurationSetting.Label);

                    if (IsKeyVaultSetting(configurationSetting))
                    {
                        appConfigItem.Value = FromKeyVaultReference(configurationSetting.Value);
                        appConfigItem.KeyVault = true;

                        if (ConsoleOutput)
                            _console.WriteLine($"Exporting AppConfiguration KeyVault Reference '{appConfigItem.Key}'");
                    }
                    else
                    {
                        if (ConsoleOutput) _console.WriteLine($"Exporting AppConfiguration Item '{appConfigItem.Key}'");
                    }

                    configItems.Add(appConfigItem);
                }

                return await Task.FromResult(ToJson(configItems));
            }
            catch (AuthenticationFailedException ex)
            {
                _console.ForegroundColor = ConsoleColor.DarkRed;
                _console.WriteLine($"Error Authenticating to App Config or KeyVault: \n {ex.Message}");
                throw;
            }
        }

        public static string ToJson(List<AppConfigItem> configItems)
        {
            return JsonConvert.SerializeObject(configItems.OrderBy(p => p.Key),
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
        }
    }
}