using System;
using System.IO;
using RapidZ.Core.Logging.Abstractions;
using RapidZ.Core.Logging.Models;
using RapidZ.Core.Logging.Services;

namespace RapidZ.Core.Logging.Utilities
{
    /// <summary>
    /// Utility methods for common logging operations
    /// </summary>
    public static class LoggingUtilities
    {
        /// <summary>
        /// Logs a skipped dataset for Export operations
        /// </summary>
        /// <param name="combinationNumber">The combination number</param>
        /// <param name="rowCount">The row count</param>
        /// <param name="parameters">The process parameters</param>
        /// <param name="reason">The reason for skipping (e.g., "RowLimit", "NoData")</param>
        public static void LogExportSkippedDataset(int combinationNumber, long rowCount, ProcessParameters parameters, string reason)
        {
            var datasetInfo = new SkippedDatasetInfo
            {
                ModuleType = "Export",
                CombinationNumber = combinationNumber,
                RowCount = rowCount,
                Parameters = parameters ?? new ProcessParameters(),
                Reason = reason ?? "Unknown"
            };

            var datasetLogger = LoggerFactory.GetDatasetLogger();
            datasetLogger.LogSkippedDataset(datasetInfo);
        }

        /// <summary>
        /// Logs a skipped dataset for Import operations
        /// </summary>
        /// <param name="combinationNumber">The combination number</param>
        /// <param name="rowCount">The row count</param>
        /// <param name="parameters">The process parameters</param>
        /// <param name="reason">The reason for skipping (e.g., "RowLimit", "NoData")</param>
        public static void LogImportSkippedDataset(int combinationNumber, long rowCount, ProcessParameters parameters, string reason)
        {
            var datasetInfo = new SkippedDatasetInfo
            {
                ModuleType = "Import",
                CombinationNumber = combinationNumber,
                RowCount = rowCount,
                Parameters = parameters ?? new ProcessParameters(),
                Reason = reason ?? "Unknown"
            };

            var datasetLogger = LoggerFactory.GetDatasetLogger();
            datasetLogger.LogSkippedDataset(datasetInfo);
        }

        /// <summary>
        /// Logs a processing summary for Export operations
        /// </summary>
        /// <param name="totalCombinations">Total number of combinations</param>
        /// <param name="filesGenerated">Number of files generated</param>
        /// <param name="combinationsSkipped">Number of combinations skipped</param>
        public static void LogExportProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            var summary = new DatasetProcessingSummary
            {
                ModuleType = "Export",
                TotalCombinations = totalCombinations,
                FilesGenerated = filesGenerated,
                CombinationsSkipped = combinationsSkipped
            };

            var datasetLogger = LoggerFactory.GetDatasetLogger();
            datasetLogger.LogProcessingSummary(summary);
        }

        /// <summary>
        /// Logs a processing summary for Import operations
        /// </summary>
        /// <param name="totalCombinations">Total number of combinations</param>
        /// <param name="filesGenerated">Number of files generated</param>
        /// <param name="combinationsSkipped">Number of combinations skipped</param>
        public static void LogImportProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped)
        {
            var summary = new DatasetProcessingSummary
            {
                ModuleType = "Import",
                TotalCombinations = totalCombinations,
                FilesGenerated = filesGenerated,
                CombinationsSkipped = combinationsSkipped
            };

            var datasetLogger = LoggerFactory.GetDatasetLogger();
            datasetLogger.LogProcessingSummary(summary);
        }

        /// <summary>
        /// Formats file size in a human-readable format
        /// </summary>
        /// <param name="sizeInBytes">The file size in bytes</param>
        /// <returns>A formatted file size string</returns>
        public static string FormatFileSize(long sizeInBytes)
        {
            if (sizeInBytes >= 1024 * 1024 * 1024)
                return $"{sizeInBytes / (1024.0 * 1024 * 1024):F1} GB";
            if (sizeInBytes >= 1024 * 1024)
                return $"{sizeInBytes / (1024.0 * 1024):F1} MB";
            if (sizeInBytes >= 1024)
                return $"{sizeInBytes / 1024.0:F1} KB";
            return $"{sizeInBytes} bytes";
        }

        /// <summary>
        /// Gets the file size of the specified file
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>The file size in bytes, or 0 if the file doesn't exist</returns>
        public static long GetFileSize(string filePath)
        {
            try
            {
                return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Creates a timer for the specified operation using the appropriate module logger
        /// </summary>
        /// <param name="moduleType">The module type (Export, Import, Cancellation)</param>
        /// <param name="operationName">The name of the operation</param>
        /// <param name="processId">The process ID</param>
        /// <returns>An operation timer</returns>
        public static IOperationTimer StartModuleTimer(string moduleType, string operationName, string processId)
        {
            var logger = LoggerFactory.GetModuleLogger(moduleType);
            return logger.StartTimer(operationName, processId);
        }
    }
}