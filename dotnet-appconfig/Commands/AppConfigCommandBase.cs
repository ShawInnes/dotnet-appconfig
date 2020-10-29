using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace ConfigManager
{
    /// <summary>
    /// This base type provides shared functionality.
    /// Also, declaring <see cref="HelpOptionAttribute"/> on this type means all types that inherit from it
    /// will automatically support '--help'
    /// </summary>
    [HelpOption("--help")]
    public abstract class AppConfigCommandBase
    {
        [Option("-v|--verbose")]
        public bool Verbose { get; set; } = false;

        [Option("-q|--quiet")]
        public bool Quiet { get; set; } = false;

        public IConsole Console { get; }

        public AppConfigCommandBase(IConsole console)
        {
            Console = console;
        }

        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            return Task.FromResult(1);
        }
    }
}
