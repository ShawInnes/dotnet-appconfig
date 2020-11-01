using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name="secret", Description = "Manage Azure KeyVault secrets")]
    [Subcommand(typeof(SecretImportCommand))]
    public class SecretCommand : AppConfigCommandBase
    {
        public SecretCommand(IConsole console) : base(console)
        {
        }

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }
    }

    [Command(Name = "import", Description = "Import Secrets from a Keepass file to Azure KeyVault")]
    public class SecretImportCommand : AppConfigCommandBase
    {
        public SecretImportCommand(IConsole console) : base(console)
        {
        }
    }
}
