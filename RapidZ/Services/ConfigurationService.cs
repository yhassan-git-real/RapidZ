using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RapidZ.Models;
using System;
using System.IO;

namespace RapidZ.Services;

// Service to handle configuration loading and access
public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;

    // Public property for accessing all app settings
    public AppSettings AppSettings { get; private set; } = new();

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        // Build configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            
        // Load settings
        LoadSettings();
    }
    
    // Reload configuration from file
    public void ReloadConfiguration()
    {
        try
        {
            LoadSettings();
            _logger.LogInformation("Configuration reloaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration reload failed");
            throw;
        }
    }
    
    // Get Excel output directory path, create if not exists
    public string GetExcelOutputPath()
    {
        var path = AppSettings.Paths.ExcelOutput;
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        return path;
    }
    
    // Get log files directory path, create if not exists
    public string GetLogFilesPath()
    {
        var path = AppSettings.Paths.LogFiles;
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        return path;
    }
    
    // Load settings from configuration
    private void LoadSettings()
    {
        AppSettings = new AppSettings();
        _configuration.Bind(AppSettings);
        _logger.LogInformation("Configuration loaded");
    }
}
