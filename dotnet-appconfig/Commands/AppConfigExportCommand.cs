using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "export", Description = "Export App Settings from Azure App Configuration to JSON")]
    public class AppConfigExportCommand : ImportExportCommandBase
    {
        [Option("--export-file=<path>", Description = "Path to export file in JSON format")]
        [Required]
        public string ExportFile { get; set; }

        [Option("--force", Description = "Overwrite existing files")]
        public bool Force { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CanExecute())
                return await Task.FromResult(1);
            
            var appConfigService = new AppConfigService(Console,
                ConfigurationClientHelpers.GetConfigurationClientByConnectionString(AppConfigConnectionString),
                new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"),
                    new DefaultAzureCredential(includeInteractiveCredentials: true)));
            
            appConfigService.ConsoleOutput = !Quiet;

            await appConfigService.ExportAppConfigurationToFile(AppConfigConnectionString,
                ExportFile);

            return await Task.FromResult(0);
        }

        protected override bool CanExecute()
        {
            if (File.Exists(ExportFile) && !Force)
            {
                if (!Quiet)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"The file '{ExportFile}' already exists.");
                    Console.ResetColor();
                }

                return false;
            }

            return true;
        }

        public AppConfigExportCommand(IConsole console) : base(console)
        {
        }
    }
}