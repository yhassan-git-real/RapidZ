using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RapidZ.Views.Models;
using System;
using System.IO;

namespace RapidZ.Core.Services;

// Service to handle configuration loading and access
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;

    // Public property for accessing all app settings
    public AppSettings AppSettings { get; private set; } = new();

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
        AppSettings = new AppSettings();
        
        // Try to bind configuration, but use defaults if sections don't exist
        try
        {
            _configuration.Bind(AppSettings);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Some configuration sections not found, using defaults");
            System.Diagnostics.Debug.WriteLine($"Some configuration sections not found, using defaults: {ex.Message}");
        }
        
        // Set default values if not configured (following TradeDataHub approach)
        if (string.IsNullOrEmpty(AppSettings.Paths.ExcelOutput))
        {
            AppSettings.Paths.ExcelOutput = Path.Combine(Directory.GetCurrentDirectory(), "EXPORT_Excel");
        }
        
        if (string.IsNullOrEmpty(AppSettings.Paths.LogFiles))
        {
            AppSettings.Paths.LogFiles = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        }
        
        _logger?.LogInformation("Configuration loaded with defaults where needed");
        System.Diagnostics.Debug.WriteLine("Configuration loaded with defaults where needed");
    }
}
