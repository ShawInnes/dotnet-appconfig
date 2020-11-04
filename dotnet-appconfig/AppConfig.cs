using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigManager
{
    [Command("appconfig")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(AppConfigImportCommand),
        typeof(AppConfigExportCommand),
        typeof(AppConfigValidateCommand),
        typeof(SecretCommand)
    )]
    class AppConfig : AppConfigCommandBase
    {
        public static async Task<int> Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .BuildServiceProvider();

            var app = new CommandLineApplication<AppConfig>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            try
            {
                return await app.ExecuteAsync(args);
            }
            catch (Exception e)
            {
                System.Console.ForegroundColor = ConsoleColor.DarkRed;
                System.Console.WriteLine(e.Message);
                System.Console.ResetColor();
                return await Task.FromResult(1);
            }
        }

        public AppConfig(IConsole console) : base(console)
        {
        }

        private static string GetVersion() => typeof(AppConfig).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }
    }
}
