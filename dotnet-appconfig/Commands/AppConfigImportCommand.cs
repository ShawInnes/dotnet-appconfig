using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "import", Description = "Import App Settings from JSON to Azure App Configuration")]
    public class AppConfigImportCommand : ImportExportCommandBase
    {
        private readonly IAppConfigService _appConfigService;

        [Option("--import-file=<path>")]
        [Required]
        [FileExists]
        public string ImportFile { get; set; }

        [Option("--keyvault-name=<name>", Description = "Azure KeyVault Name, excluding the https:// prefix and .vault.azure.net")]
        [Required]
        public string KeyVaultName { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            Console.WriteLine("AppConfigImportCommand OnExecuteAsync");

            if (!CanExecute())
                return await Task.FromResult(1);

            _appConfigService.ConsoleOutput = !Quiet;

            if (!string.IsNullOrEmpty(AppConfigName))
            {
                await _appConfigService.ImportAppConfigurationFromFileByName(AppConfigName, KeyVaultName, ImportFile, DryRun);
            }
            else if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                await _appConfigService.ImportAppConfigurationFromFileByConnectionString(AppConfigConnectionString, KeyVaultName, ImportFile, DryRun);
            }
            else
            {
                throw new InvalidOperationException("No App Config Name or Connection String specified");
            }

            return await Task.FromResult(0);
        }

        public AppConfigImportCommand(IConsole console, IAppConfigService appConfigService) : base(console)
        {
            _appConfigService = appConfigService;
            Console.WriteLine("AppConfigImportCommand Constructor");
        }
    }
}
