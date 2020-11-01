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
    [Subcommand(typeof(AppConfigImportCommand), typeof(AppConfigExportCommand), typeof(SecretCommand))]
    class AppConfig : AppConfigCommandBase
    {
        public static Task<int> Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .AddTransient<IAppConfigService, AppConfigService>()
                .BuildServiceProvider();

            var app = new CommandLineApplication<AppConfig>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            return app.ExecuteAsync(args);
        }

        public AppConfig(IConsole console) : base(console)
        {
        }

        private static string GetVersion() => typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(1);
        }
    }
}
