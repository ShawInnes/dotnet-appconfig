using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    public abstract class ImportExportCommandBase : AppConfigCommandBase
    {
        [Option("-c|--connection-string=<connection_string>",
            optionType: CommandOptionType.SingleValue,
            description: "Azure App Configuration connection string")]
        [Required]
        public string AppConfigConnectionString { get; set; }

        protected ImportExportCommandBase(IConsole console) : base(console)
        {
        }
    }
}
