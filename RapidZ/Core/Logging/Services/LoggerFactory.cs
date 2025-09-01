using System;
using System.Collections.Concurrent;
using System.Threading;
using RapidZ.Core.Logging.Abstractions;
using RapidZ.Core.Logging.Core;

namespace RapidZ.Core.Logging.Services
{
    /// <summary>
    /// Factory service for creating and managing logger instances
    /// </summary>
    public static class LoggerFactory
    {
        private static readonly ConcurrentDictionary<string, Lazy<IModuleLogger>> _moduleLoggers = new();
        private static readonly Lazy<IDatasetLogger> _datasetLogger = new(() => new DatasetLoggerImpl());

        /// <summary>
        /// Gets or creates a module logger for the specified module type
        /// </summary>
        /// <param name="moduleType">The module type (e.g., "Export", "Import", "Cancellation")</param>
        /// <param name="logFileExtension">Optional file extension for log files</param>
        /// <returns>A module logger instance</returns>
        public static IModuleLogger GetModuleLogger(string moduleType, string? logFileExtension = null)
        {
            if (string.IsNullOrWhiteSpace(moduleType))
                throw new ArgumentException("Module type cannot be null or empty", nameof(moduleType));

            var key = $"{moduleType}_{logFileExtension ?? ".txt"}";
            
            return _moduleLoggers.GetOrAdd(key, _ => 
                new Lazy<IModuleLogger>(() => new ModuleLoggerImpl(moduleType, logFileExtension), 
                    LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        /// <summary>
        /// Gets the module logger for Export operations
        /// </summary>
        /// <returns>A module logger for Export operations</returns>
        public static IModuleLogger GetExportLogger()
        {
            return GetModuleLogger("Export");
        }

        /// <summary>
        /// Gets the module logger for Import operations
        /// </summary>
        /// <returns>A module logger for Import operations</returns>
        public static IModuleLogger GetImportLogger()
        {
            return GetModuleLogger("Import");
        }

        /// <summary>
        /// Gets the module logger for Cancellation operations
        /// </summary>
        /// <returns>A module logger for Cancellation operations</returns>
        public static IModuleLogger GetCancellationLogger()
        {
            return GetModuleLogger("Cancellation");
        }

        /// <summary>
        /// Gets the dataset logger for skipped dataset operations
        /// </summary>
        /// <returns>A dataset logger instance</returns>
        public static IDatasetLogger GetDatasetLogger()
        {
            return _datasetLogger.Value;
        }

        /// <summary>
        /// Creates a general-purpose logger with the specified prefix
        /// </summary>
        /// <param name="logPrefix">The prefix for log files</param>
        /// <param name="logFileExtension">Optional file extension for log files</param>
        /// <returns>A general logger instance</returns>
        public static ILogger CreateLogger(string logPrefix, string? logFileExtension = null)
        {
            if (string.IsNullOrWhiteSpace(logPrefix))
                throw new ArgumentException("Log prefix cannot be null or empty", nameof(logPrefix));

            return new GeneralLoggerImpl(logPrefix, logFileExtension);
        }

        /// <summary>
        /// Disposes all cached logger instances and clears the cache
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var lazyLogger in _moduleLoggers.Values)
            {
                if (lazyLogger.IsValueCreated)
                {
                    lazyLogger.Value?.Dispose();
                }
            }
            _moduleLoggers.Clear();

            // DatasetLogger doesn't require disposal
        }
    }

    /// <summary>
    /// General-purpose logger implementation for non-module specific logging
    /// </summary>
    internal sealed class GeneralLoggerImpl : BaseLogger
    {
        private readonly string _logPrefix;

        protected override string LogFilePrefix => _logPrefix;
        protected override string ModuleName => _logPrefix;

        public GeneralLoggerImpl(string logPrefix, string? logFileExtension = null) 
            : base(logFileExtension)
        {
            _logPrefix = logPrefix ?? throw new ArgumentNullException(nameof(logPrefix));
        }
    }
}