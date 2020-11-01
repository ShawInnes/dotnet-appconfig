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
        [Required]
        public string AppConfigConnectionString { get; set; }
        
        [Option("--keyvault-name=<name>",
            Description = "Azure KeyVault Name, excluding the https:// prefix and .vault.azure.net")]
        [Required]
        public string KeyVaultName { get; set; }

        [Option("--dry-run")]
        public bool DryRun { get; set; }

        protected ImportExportCommandBase(IConsole console) : base(console)
        {
        }

        protected override bool CanExecute()
        {
            if (string.IsNullOrEmpty(AppConfigConnectionString))
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
