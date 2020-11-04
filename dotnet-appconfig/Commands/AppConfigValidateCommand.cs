using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigManager.Services;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace ConfigManager
{
    [Command(Name = "validate", Description = "Validate an App Settings JSON file")]
    public class AppConfigValidateCommand : AppConfigCommandBase
    {
        [Option("--import-file=<path>")]
        [Required]
        [FileExists]
        public string ImportFile { get; set; }

        public AppConfigValidateCommand(IConsole console) : base(console)
        {
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (!CanExecute())
                return await Task.FromResult(1);

            Console.WriteLine($"Validating file '{ImportFile}'");

            var json = await File.ReadAllTextAsync(ImportFile);
            var (items, errors) = AppConfigService.ReadAppConfigItems(json);
            if (errors.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"File '{ImportFile}' is not a valid JSON file.");

                foreach (var error in errors)
                    Console.WriteLine(error);

                return await Task.FromResult(1);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"File '{ImportFile}' is a valid JSON file.");

            return await Task.FromResult(0);
        }
    }
}
