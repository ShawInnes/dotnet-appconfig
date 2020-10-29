using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Description = "Export App Settings from Azure to JSON")]
    public class ExportCommand : ImportExportCommandBase
    {
        private readonly IAppConfigService _appConfigService;

        [Option("--export-file=<path>", Description = "Path to export file in JSON format")]
        [Required]
        public string ExportFile { get; set; }

        [Option("--force", Description = "Overwrite existing files")]
        public bool Force { get; set; }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            _appConfigService.ConsoleOutput = !Quiet;

            if (File.Exists(ExportFile) && !Force)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"The file '{ExportFile}' already exists.");
                Console.ResetColor();

                return await Task.FromResult(1);
            }

            await _appConfigService.ExportAppConfigurationToFile(AppConfigConnectionString, ExportFile);

            return await Task.FromResult(0);
        }

        public ExportCommand(IConsole console, IAppConfigService appConfigService) : base(console)
        {
            _appConfigService = appConfigService;
            _appConfigService.ConsoleOutput = !Quiet;
        }
    }
}
