using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "cleanup", Description = "Clean Up App Settings from Azure App Configuration")]
    public class AppConfigCleanUpCommand : ImportExportCommandBase
    {
        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CanExecute())
                return await Task.FromResult(1);

            var appConfigService = new AppConfigService(Console,
                ConfigurationClientHelpers.GetConfigurationClientByConnectionString(AppConfigConnectionString),
                new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"),
                    new DefaultAzureCredential(includeInteractiveCredentials: true)));

            appConfigService.ConsoleOutput = !Quiet;

            await appConfigService.CleanUpConfiguration(AppConfigConnectionString, KeyVaultName, DryRun);

            return await Task.FromResult(0);
        }

        public AppConfigCleanUpCommand(IConsole console) : base(console)
        {
        }
    }
}