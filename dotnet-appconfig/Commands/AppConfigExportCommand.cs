using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "export", Description = "Export App Settings from Azure App Configuration to JSON")]
    public class AppConfigExportCommand : ImportExportCommandBase
    {
        private readonly IAppConfigService _appConfigService;

        [Option("--export-file=<path>", Description = "Path to export file in JSON format")]
        [Required]
        public string ExportFile { get; set; }

        [Option("--force", Description = "Overwrite existing files")]
        public bool Force { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CanExecute())
                return await Task.FromResult(1);

            _appConfigService.ConsoleOutput = !Quiet;

            if (!string.IsNullOrEmpty(AppConfigName))
            {
                await _appConfigService.ExportAppConfigurationToFileByName(AppConfigName, ExportFile);
            }
            else if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                await _appConfigService.ExportAppConfigurationToFileByConnectionString(AppConfigConnectionString, ExportFile);
            }
            else
            {
                throw new InvalidOperationException("No App Config Name or Connection String specified");
            }

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

        public AppConfigExportCommand(IConsole console, IAppConfigService appConfigService) : base(console)
        {
            _appConfigService = appConfigService;
            _appConfigService.ConsoleOutput = !Quiet;
        }
    }
}
