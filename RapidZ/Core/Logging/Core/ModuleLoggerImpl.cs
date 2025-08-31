using System;
using System.Text;
using RapidZ.Core.Logging.Abstractions;
using RapidZ.Core.Logging.Models;

namespace RapidZ.Core.Logging.Core
{
    /// <summary>
    /// Implementation of module-specific logging functionality
    /// </summary>
    public sealed class ModuleLoggerImpl : BaseLogger, IModuleLogger
    {
        private readonly string _modulePrefix;

        /// <summary>
        /// Gets the file prefix for log files
        /// </summary>
        protected override string LogFilePrefix => $"{_modulePrefix}_Log";

        /// <summary>
        /// Gets the module name for this logger
        /// </summary>
        protected override string ModuleName => _modulePrefix;

        /// <summary>
        /// Gets the module name associated with this logger
        /// </summary>
        string IModuleLogger.ModuleName => _modulePrefix;

        /// <summary>
        /// Initializes a new instance of the ModuleLoggerImpl class
        /// </summary>
        /// <param name="modulePrefix">The prefix to use for log files and module identification</param>
        /// <param name="logFileExtension">The file extension for log files (optional, defaults to .txt)</param>
        public ModuleLoggerImpl(string modulePrefix, string? logFileExtension = null) 
            : base(logFileExtension)
        {
            _modulePrefix = modulePrefix ?? throw new ArgumentNullException(nameof(modulePrefix));
            
            // Now that _modulePrefix is set, initialize the log file name
            UpdateLogFileName();
        }

        public void LogProcessStart(string processName, string parameters, string processId)
        {
            LogInfo("================================================================================", processId);
            LogInfo($"🚀 PROCESS START: {processName}", processId);
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                LogInfo($"📋 Parameters: {parameters}", processId);
            }
            LogInfo("--------------------------------------------------------------------------------", processId);
        }

        public void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId)
        {
            LogInfo("--------------------------------------------------------------------------------", processId);
            LogInfo($"✅ PROCESS COMPLETE: {processName}", processId);
            LogInfo($"⏱️  Total Time: {elapsed:mm\\:ss\\.fff}", processId);
            if (!string.IsNullOrWhiteSpace(result))
            {
                LogInfo($"📊 Result: {result}", processId);
            }
            LogInfo("================================================================================", processId);
        }

        public void LogProcessCompletion(string processName, string parameters, TimeSpan elapsed, string result, string processId)
        {
            var message = new StringBuilder($"Process Completed: {processName}");
            
            if (!string.IsNullOrWhiteSpace(parameters))
            {
                message.Append($" | Parameters: {parameters}");
            }
            
            message.Append($" | Elapsed: {elapsed:hh\\:mm\\:ss\\.fff}");
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                message.Append($" | Result: {result}");
            }
            
            LogInfo(message.ToString(), processId);
        }

        public void LogStep(string stepName, string details, string processId)
        {
            var message = $"  ➤ {stepName}";
            if (!string.IsNullOrWhiteSpace(details))
            {
                message += $": {details}";
            }
            LogInfo(message, processId);
        }

        public void LogDetailedParameters(ProcessParameters parameters, string processId)
        {
            if (parameters == null) return;
            
            var message = $"Detailed Parameters: {parameters}";
            LogInfo(message, processId);
        }

        public void LogStoredProcedure(string spName, string parameters, TimeSpan elapsed, string processId)
        {
            LogInfo($"  ➤ Database: Executing stored procedure", processId);
        }

        public void LogStoredProcedureExecution(string procedureName, string parameters, TimeSpan elapsed, string processId)
        {
            var message = $"Stored Procedure: {procedureName} | Parameters: {parameters} | Elapsed: {elapsed:hh\\:mm\\:ss\\.fff}";
            LogInfo(message, processId);
        }

        public void LogDataReader(string viewName, string orderBy, long rowCount, string processId)
        {
            LogInfo($"Data Reader: {viewName} | Order By: {orderBy} | Rows: {rowCount:N0}", processId);
        }

        public void LogDataReaderOperation(string operation, int recordCount, TimeSpan elapsed, string processId)
        {
            var message = $"Data Reader {operation}: {recordCount:N0} records | Elapsed: {elapsed:hh\\:mm\\:ss\\.fff}";
            LogInfo(message, processId);
        }

        public void LogExcelFileCreationStart(string fileName, string processId)
        {
            LogInfo($"  📋 Creating Excel file: {fileName}", processId);
        }

        public void LogExcelFileCreationComplete(string fileName, TimeSpan elapsed, string processId)
        {
            LogInfo($"  💾 File Save Completed | ⏱️ {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogExcelResult(string fileName, TimeSpan elapsed, long rowCount, string processId)
        {
            LogInfo($"  ✅ Excel Complete: {fileName} | ⏱️ {elapsed:mm\\:ss\\.fff} | 📊 {rowCount:N0} records", processId);
        }

        public void LogExcelResult(string fileName, int rowCount, long fileSize, string processId)
        {
            var fileSizeFormatted = fileSize > 1024 * 1024 ? $"{fileSize / (1024.0 * 1024):F1} MB" : $"{fileSize / 1024.0:F1} KB";
            LogInfo($"  ✅ Excel Complete: {fileName} | 📊 {rowCount:N0} records | 💾 {fileSizeFormatted}", processId);
        }

        public void LogFileSaveOperation(string fileName, bool success, string? errorMessage, string processId)
        {
            if (success)
            {
                LogInfo($"  💾 File Saved: {fileName}", processId);
            }
            else
            {
                LogError($"  ❌ File Save Failed: {fileName} | Error: {errorMessage ?? "Unknown error"}", processId);
            }
        }

        public void LogSkippedItem(string itemName, string reason, string processId)
        {
            LogWarning($"  ⚠️  Skipped: {itemName} | Reason: {reason}", processId);
        }

        public void LogProcessingSummary(ProcessingSummary summary)
        {
            if (summary == null) return;
            
            var message = new StringBuilder("Processing Summary:");
            message.AppendLine();
            message.AppendLine($"  Total Combinations: {summary.TotalCombinations:N0}");
            message.AppendLine($"  Files Generated: {summary.FilesGenerated:N0}");
            message.AppendLine($"  Combinations Skipped: {summary.CombinationsSkipped:N0}");
            message.AppendLine($"  Success Rate: {summary.SuccessRate:P2}");
            message.AppendLine($"  Total Elapsed Time: {summary.TotalElapsed:hh\\:mm\\:ss\\.fff}");
            
            LogInfo(message.ToString());
        }

        public void LogProcessingSummary(ProcessingSummary summary, string processId)
        {
            if (summary == null) return;
            
            LogInfo("--------------------------------------------------------------------------------", processId);
            LogInfo("📊 PROCESSING SUMMARY:", processId);
            LogInfo($"  📋 Total Combinations: {summary.TotalCombinations:N0}", processId);
            LogInfo($"  📁 Files Generated: {summary.FilesGenerated:N0}", processId);
            LogInfo($"  ⚠️  Combinations Skipped: {summary.CombinationsSkipped:N0}", processId);
            LogInfo($"  ✅ Success Rate: {summary.SuccessRate:P2}", processId);
            LogInfo($"  ⏱️  Total Time: {summary.TotalElapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogExcelFileCreation(string fileName, TimeSpan elapsed, long rowCount, string processId)
        {
            LogInfo($"  📋 Excel Created: {fileName} | 📊 {rowCount:N0} records | ⏱️ {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogFileSave(string operation, TimeSpan elapsed, string processId)
        {
            LogInfo($"  💾 File Save: {operation} | ⏱️ {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogSkipped(string fileName, long rowCount, string reason, string processId)
        {
            LogWarning($"  ⚠️  Skipped: {fileName} | 📊 {rowCount:N0} records | Reason: {reason}", processId);
        }

        public void LogSummary(ProcessingSummary summary)
        {
            LogInfo("--------------------------------------------------------------------------------");
            LogInfo("📊 PROCESSING SUMMARY:");
            LogInfo($"  📋 Total Combinations: {summary.TotalCombinations:N0}");
            LogInfo($"  📁 Files Generated: {summary.FilesGenerated:N0}");
            LogInfo($"  ⚠️  Combinations Skipped: {summary.CombinationsSkipped:N0}");
            LogInfo($"  ✅ Success Rate: {summary.SuccessRate:P2}");
            LogInfo($"  ⏱️  Total Time: {summary.TotalElapsed:mm\\:ss\\.fff}");
        }

        public IOperationTimer StartTimer(string operationName, string processId)
        {
            LogProcessStart(operationName, string.Empty, processId);
            return new OperationTimer(this, operationName, processId);
        }
    }
}