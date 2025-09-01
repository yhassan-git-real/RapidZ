using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Database;
using RapidZ.Config;
using RapidZ.Features.Export;
using RapidZ.Features.Import;

namespace RapidZ.Core.Services
{
    /// <summary>
    /// Service for caching configuration objects to improve performance
    /// </summary>
    public class ConfigurationCacheService
    {
        private static readonly ConcurrentDictionary<string, object> _configCache = new();
        private static readonly object _lockObject = new();
        
        // Keep legacy instance support for backward compatibility
        private static readonly Lazy<ConfigurationCacheService> _instance = new(() => new ConfigurationCacheService());
        public static ConfigurationCacheService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, object> _cache = new();

        private ConfigurationCacheService()
        {
        }

        /// <summary>
        /// Get or load ExcelFormatSettings with caching (static method like TradeDataHub)
        /// </summary>
        public static ExportExcelFormatSettings GetExcelFormatSettings()
        {
            const string cacheKey = "ExcelFormatSettings";
            return (ExportExcelFormatSettings)_configCache.GetOrAdd(cacheKey, _ => LoadExcelFormatSettings());
        }

        /// <summary>
        /// Get or load ExportSettings with caching (static method like TradeDataHub)
        /// </summary>
        public static ExportSettings GetExportSettings()
        {
            const string cacheKey = "ExportSettings";
            return (ExportSettings)_configCache.GetOrAdd(cacheKey, _ => LoadExportSettings());
        }

        /// <summary>
        /// Get or load ImportSettings with caching (static method like TradeDataHub)
        /// </summary>
        public static ImportSettings GetImportSettings()
        {
            const string cacheKey = "ImportSettings";
            return (ImportSettings)_configCache.GetOrAdd(cacheKey, _ => LoadImportSettings());
        }

        /// <summary>
        /// Get or load SharedDatabaseSettings with caching (static method like TradeDataHub)
        /// </summary>
        public static SharedDatabaseSettings GetSharedDatabaseSettings()
        {
            const string cacheKey = "SharedDatabaseSettings";
            return (SharedDatabaseSettings)_configCache.GetOrAdd(cacheKey, _ => LoadSharedDatabaseSettings());
        }

        /// <summary>
        /// Get or load ImportExcelFormatSettings with caching (static method like TradeDataHub)
        /// </summary>
        public static RapidZ.Config.ImportExcelFormatSettings GetImportExcelFormatSettings()
        {
            const string cacheKey = "ImportExcelFormatSettings";
            return (RapidZ.Config.ImportExcelFormatSettings)_configCache.GetOrAdd(cacheKey, _ => LoadImportExcelFormatSettings());
        }

        /// <summary>
        /// Clear configuration cache (useful for testing or configuration updates)
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _configCache.Clear();
            }
        }

        /// <summary>
        /// Remove specific configuration from cache
        /// </summary>
        public static void InvalidateCache(string cacheKey)
        {
            _configCache.TryRemove(cacheKey, out _);
        }

        #region Static Private Loading Methods (TradeDataHub style)

        private static ExportExcelFormatSettings LoadExcelFormatSettings()
        {
            const string jsonFileName = "Config/ExportExcelFormatSettings.json";
            if (!File.Exists(jsonFileName))
                throw new FileNotFoundException($"Excel formatting file '{jsonFileName}' not found.");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonFileName, false);
            var config = builder.Build();
            return config.Get<ExportExcelFormatSettings>()!;
        }

        private static ExportSettings LoadExportSettings()
        {
            const string json = "Config/export.appsettings.json";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<ExportSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind ExportSettingsRoot");
            return root.ExportSettings;
        }

        private static ImportSettings LoadImportSettings()
        {
            const string json = "Config/import.appsettings.json";
            if (!File.Exists(json)) throw new FileNotFoundException($"Missing import settings file: {json}");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<ImportSettingsRoot>();
            if (root == null) throw new InvalidOperationException("Failed to bind ImportSettingsRoot");
            return root.ImportSettings;
        }

        private static SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }

        private static RapidZ.Config.ImportExcelFormatSettings LoadImportExcelFormatSettings()
        {
            const string json = "Config/ImportExcelFormatSettings.json";
            if (!File.Exists(json)) 
                throw new FileNotFoundException($"Missing import formatting file: {json}");
            
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(json, false);
                var cfg = builder.Build();
                var settings = cfg.Get<RapidZ.Config.ImportExcelFormatSettings>();
                
                if (settings == null)
                    throw new InvalidOperationException($"Failed to bind ImportExcelFormatSettings from {json}");
                
                // Validate required properties
                if (string.IsNullOrEmpty(settings.FontName))
                    throw new InvalidOperationException("FontName is required in ImportExcelFormatSettings");
                if (string.IsNullOrEmpty(settings.HeaderBackgroundColor))
                    throw new InvalidOperationException("HeaderBackgroundColor is required in ImportExcelFormatSettings");
                if (settings.DateColumns == null)
                    throw new InvalidOperationException("DateColumns is required in ImportExcelFormatSettings");
                if (settings.TextColumns == null)
                    throw new InvalidOperationException("TextColumns is required in ImportExcelFormatSettings");
                
                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading ImportExcelFormatSettings from {json}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Legacy Instance Methods (kept for backward compatibility)

        /// <summary>
        /// Gets a configuration object from cache or loads it from file if not cached
        /// </summary>
        /// <typeparam name="T">The type of configuration object to retrieve</typeparam>
        /// <param name="configFilePath">The path to the configuration file</param>
        /// <param name="sectionName">The section name in the configuration file (optional)</param>
        /// <returns>The configuration object of type T</returns>
        public T GetConfiguration<T>(string configFilePath, string? sectionName = null) where T : class, new()
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                throw new ArgumentException("Configuration file path cannot be null or empty", nameof(configFilePath));
            }

            string cacheKey = $"{typeof(T).Name}_{configFilePath}_{sectionName ?? "root"}";

            return (T)_cache.GetOrAdd(cacheKey, _ => LoadConfiguration<T>(configFilePath, sectionName));
        }



        private T LoadConfiguration<T>(string configFilePath, string? sectionName) where T : class, new()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    return new T();
                }

                var builder = new ConfigurationBuilder()
                    .AddJsonFile(configFilePath, optional: false, reloadOnChange: false);

                var configuration = builder.Build();

                T configObject;
                if (!string.IsNullOrEmpty(sectionName))
                {
                    configObject = configuration.GetSection(sectionName).Get<T>() ?? new T();
                }
                else
                {
                    configObject = configuration.Get<T>() ?? new T();
                }

                return configObject;
            }
            catch (Exception)
            {
                return new T();
            }
        }

        #endregion
    }
}