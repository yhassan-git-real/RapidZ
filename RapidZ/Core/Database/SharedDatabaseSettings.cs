namespace RapidZ.Core.Database
{
    public class SharedDatabaseSettingsRoot
    {
        public SharedDatabaseSettings DatabaseConfig { get; set; } = new SharedDatabaseSettings();
    }

    public class SharedDatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string LogDirectory { get; set; } = string.Empty;
        public int CommandTimeoutSeconds { get; set; }
    }
}