using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    public abstract class ImportExportCommandBase : AppConfigCommandBase
    {
        [Option("-c|--connection-string=<connection_string>",
            Description = "Azure App Configuration connection string")]
        public string AppConfigConnectionString { get; set; }

        [Option("-n|--app-config-name=<app_config_name>",
            Description = "Azure App Configuration name")]
        public string AppConfigName { get; set; }

        [Option("--dry-run")]
        public bool DryRun { get; set; }

        protected ImportExportCommandBase(IConsole console) : base(console)
        {
        }

        protected override bool CanExecute()
        {
            Console.WriteLine("ImportExportCommandBase CanExecute");

            if (string.IsNullOrEmpty(AppConfigConnectionString) && string.IsNullOrEmpty(AppConfigName))
            {
                if (!Quiet)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"The --app-config-name or --connection-string field is required.");
                    Console.ResetColor();
                }

                return false;
            }

            return true;
        }
    }
}
