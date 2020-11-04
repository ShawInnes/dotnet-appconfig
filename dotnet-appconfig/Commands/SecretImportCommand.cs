using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    [Command(Name = "import", Description = "Import Secrets from a Keepass file to Azure KeyVault")]
    public class SecretImportCommand : AppConfigCommandBase
    {
        public SecretImportCommand(IConsole console) : base(console)
        {
        }
    }
}