namespace ConfigManager.Models
{
    public class AppConfigItem
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public bool KeyVault { get; set; }
        public bool Purge { get; set; }
    }
}
