using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace RapidZ.Core.Services;

// Service to handle configuration loading and access
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;



    public ConfigurationService(ILogger<ConfigurationService> logger = null)
    {
        _logger = logger; // May be null, handle carefully in log calls
        
        // Build configuration from specific config files (like TradeDataHub approach)
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());
            
        // Add only the specific configuration files that exist
        var configDir = Path.Combine(Directory.GetCurrentDirectory(), "Config");
        if (Directory.Exists(configDir))
        {
            // Add database config if it exists
            var dbConfigPath = Path.Combine(configDir, "database.appsettings.json");
            if (File.Exists(dbConfigPath))
                builder.AddJsonFile("Config/database.appsettings.json", optional: true, reloadOnChange: true);
                
            // Add export config if it exists  
            var exportConfigPath = Path.Combine(configDir, "export.appsettings.json");
            if (File.Exists(exportConfigPath))
                builder.AddJsonFile("Config/export.appsettings.json", optional: true, reloadOnChange: true);
                
            // Add import config if it exists
            var importConfigPath = Path.Combine(configDir, "import.appsettings.json");
            if (File.Exists(importConfigPath))
                builder.AddJsonFile("Config/import.appsettings.json", optional: true, reloadOnChange: true);
        }
        
        _configuration = builder.Build();
            
        // Load settings
        LoadSettings();
    }
    
    // Load settings from configuration
    private void LoadSettings()
    {
        // Configuration service now only manages the IConfiguration instance
        // Individual services load their own settings as needed
        _logger?.LogInformation("Configuration service initialized");
        System.Diagnostics.Debug.WriteLine("Configuration service initialized");
    }
}
