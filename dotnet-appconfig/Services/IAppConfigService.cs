using System.Threading.Tasks;

namespace ConfigManager.Services
{
    public interface IAppConfigService
    {
        Task ExportAppConfigurationToFile(string connectionString, string outputPath);
        Task ImportAppConfigurationFromFile(string connectionString, string keyVaultName, string inputPath, bool dryRun, bool strict, string separator);
        bool ConsoleOutput { get; set; }
    }
}
