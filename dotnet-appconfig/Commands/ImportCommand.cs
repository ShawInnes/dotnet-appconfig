using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Description = "Import App Settings from JSON to Azure")]
    public class ImportCommand : ImportExportCommandBase
    {
        private readonly IAppConfigService _appConfigService;

        [Option("--import-file=<path>")]
        [Required]
        [FileExists]
        public string ImportFile { get; set; }

        [Option("--keyvault-name=<name>", Description = "Azure KeyVault Name, excluding the https:// prefix and .vault.azure.net")]
        [Required]
        public string KeyVaultName { get; set; }

        [Option("--dry-run")]
        public bool DryRun { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            _appConfigService.ConsoleOutput = !Quiet;

            await _appConfigService.ImportAppConfigurationFromFile(AppConfigConnectionString, KeyVaultName, ImportFile, DryRun);

            return await Task.FromResult(0);
        }

        public ImportCommand(IConsole console, IAppConfigService appConfigService) : base(console)
        {
            _appConfigService = appConfigService;
        }
    }
}
