using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Logging.Abstractions;
using RapidZ.Core.Logging.Models;

namespace RapidZ.Core.Logging.Core
{
    /// <summary>
    /// Implementation of dataset-specific logging functionality
    /// </summary>
    public sealed class DatasetLoggerImpl : IDatasetLogger
    {
        private readonly string _logDirectory;
        private readonly object _fileLock = new();

        /// <summary>
        /// Initializes a new instance of the DatasetLoggerImpl class
        /// </summary>
        public DatasetLoggerImpl()
        {
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("Config/database.appsettings.json", optional: false);
            var cfg = builder.Build();
            
            _logDirectory = cfg["DatabaseConfig:LogDirectory"] ?? Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(_logDirectory);
        }

        public void LogSkippedDataset(SkippedDatasetInfo datasetInfo)
        {
            if (datasetInfo == null) return;

            var moduleType = datasetInfo.ModuleType?.ToLower() ?? "unknown";
            var logFileName = Path.Combine(_logDirectory, $"{char.ToUpper(moduleType[0])}{moduleType.Substring(1)}_SkippedDatasets_{DateTime.Now:yyyyMMdd}.txt");
            var logEntry = BuildSkippedDatasetLogEntry(datasetInfo);

            lock (_fileLock)
            {
                File.AppendAllText(logFileName, logEntry);
            }
        }

        public void LogProcessingSummary(DatasetProcessingSummary summary)
        {
            if (summary == null) return;

            var moduleType = summary.ModuleType?.ToLower() ?? "unknown";
            var logFileName = Path.Combine(_logDirectory, $"{char.ToUpper(moduleType[0])}{moduleType.Substring(1)}_SkippedDatasets_{DateTime.Now:yyyyMMdd}.txt");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SUMMARY {summary}\r\n";

            lock (_fileLock)
            {
                File.AppendAllText(logFileName, logEntry);
            }
        }

        private static string BuildSkippedDatasetLogEntry(SkippedDatasetInfo datasetInfo)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SKIPPED DATASET - {datasetInfo.ModuleType.ToUpper()}");
            logEntry.AppendLine($"Combination Number: {datasetInfo.CombinationNumber}");
            logEntry.AppendLine($"Row Count: {datasetInfo.FormattedReason}");
            logEntry.AppendLine($"Reason: {datasetInfo.Reason}");
            
            // Add parameter details
            var parameters = datasetInfo.Parameters;
            if (parameters != null)
            {
                if (!string.IsNullOrWhiteSpace(parameters.FromMonth) || !string.IsNullOrWhiteSpace(parameters.ToMonth))
                {
                    logEntry.AppendLine($"Period: {parameters.FromMonth} to {parameters.ToMonth}");
                }
                
                logEntry.AppendLine("Filters:");
                
                if (!string.IsNullOrWhiteSpace(parameters.HsCode))
                {
                    logEntry.AppendLine($"  HS Code: {parameters.HsCode}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.Product))
                {
                    logEntry.AppendLine($"  Product: {parameters.Product}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.Iec))
                {
                    logEntry.AppendLine($"  IEC: {parameters.Iec}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.ExporterOrImporter))
                {
                    logEntry.AppendLine($"  Party: {parameters.ExporterOrImporter}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.Country))
                {
                    logEntry.AppendLine($"  Country: {parameters.Country}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.Name))
                {
                    logEntry.AppendLine($"  Name: {parameters.Name}");
                }
                
                if (!string.IsNullOrWhiteSpace(parameters.Port))
                {
                    logEntry.AppendLine($"  Port: {parameters.Port}");
                }
            }
            
            logEntry.AppendLine("--------------------------------------------------------------------------------");
            return logEntry.ToString();
        }

        public void Dispose()
        {
            // No resources to dispose for this implementation
        }
    }
}