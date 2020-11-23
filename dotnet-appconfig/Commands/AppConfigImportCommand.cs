using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "import", Description = "Import App Settings from JSON to Azure App Configuration")]
    public class AppConfigImportCommand : ImportExportCommandBase
    {
        [Option("--import-file=<path>")]
        [Required]
        [FileExists]
        public string ImportFile { get; set; }

        [Option("--strict",
            Description = "Ensure that KeyVault values exist for App Configuration references")]
        public bool Strict { get; set; }

        [Option("--separator",
            Description = "Override the default separator character")]
        public string Separator { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CanExecute())
                return await Task.FromResult(1);

            var appConfigService = new AppConfigService(Console,
                ConfigurationClientHelpers.GetConfigurationClientByConnectionString(AppConfigConnectionString),
                new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"),
                    new DefaultAzureCredential(includeInteractiveCredentials: true)));

            appConfigService.ConsoleOutput = !Quiet;

            await appConfigService.ImportAppConfigurationFromFile(AppConfigConnectionString,
                KeyVaultName, ImportFile, DryRun, Strict, Separator);

            return await Task.FromResult(0);
        }

        public AppConfigImportCommand(IConsole console) : base(console)
        {
        }
    }
}
