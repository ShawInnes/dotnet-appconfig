using System.Threading.Tasks;

namespace ConfigManager.Services
{
    public interface IAppConfigService
    {
        Task ExportAppConfigurationToFileByConnectionString(string connectionString, string outputPath);
        Task ImportAppConfigurationFromFileByConnectionString(string connectionString, string keyVaultName, string inputPath, bool dryRun);
        Task ExportAppConfigurationToFileByName(string appConfigName, string outputPath);
        Task ImportAppConfigurationFromFileByName(string appConfigName, string keyVaultName, string inputPath, bool dryRun);
        bool ConsoleOutput { get; set; }
    }
}
