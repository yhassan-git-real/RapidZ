using System;
using RapidZ.Core.Logging.Models;

namespace RapidZ.Core.Logging.Abstractions
{
    /// <summary>
    /// Extended logging interface for module-specific operations
    /// </summary>
    public interface IModuleLogger : ILogger
    {
        /// <summary>
        /// Gets the module name associated with this logger
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Logs the start of a process with parameters
        /// </summary>
        /// <param name="processName">Name of the process being started</param>
        /// <param name="parameters">Process parameters</param>
        /// <param name="processId">Process identifier</param>
        void LogProcessStart(string processName, string parameters, string processId);

        /// <summary>
        /// Logs the completion of a process with results
        /// </summary>
        /// <param name="processName">Name of the completed process</param>
        /// <param name="elapsed">Time taken to complete the process</param>
        /// <param name="result">Process result summary</param>
        /// <param name="processId">Process identifier</param>
        void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId);

        /// <summary>
        /// Logs a step within a process
        /// </summary>
        /// <param name="stepName">Name of the step</param>
        /// <param name="details">Step details</param>
        /// <param name="processId">Process identifier</param>
        void LogStep(string stepName, string details, string processId);

        /// <summary>
        /// Logs detailed parameters for tracking
        /// </summary>
        /// <param name="parameters">The parameters to log</param>
        /// <param name="processId">Process identifier</param>
        void LogDetailedParameters(ProcessParameters parameters, string processId);

        /// <summary>
        /// Logs stored procedure execution details
        /// </summary>
        /// <param name="spName">Stored procedure name</param>
        /// <param name="parameters">Execution parameters</param>
        /// <param name="elapsed">Execution time</param>
        /// <param name="processId">Process identifier</param>
        void LogStoredProcedure(string spName, string parameters, TimeSpan elapsed, string processId);

        /// <summary>
        /// Logs data reader operation details
        /// </summary>
        /// <param name="viewName">View or table name</param>
        /// <param name="orderBy">Order by clause</param>
        /// <param name="rowCount">Number of rows processed</param>
        /// <param name="processId">Process identifier</param>
        void LogDataReader(string viewName, string orderBy, long rowCount, string processId);

        /// <summary>
        /// Logs Excel file creation start
        /// </summary>
        /// <param name="fileName">Name of the Excel file</param>
        /// <param name="processId">Process identifier</param>
        void LogExcelFileCreationStart(string fileName, string processId);

        /// <summary>
        /// Logs Excel file creation start with full file path
        /// </summary>
        /// <param name="fileName">Name of the Excel file</param>
        /// <param name="filePath">Full path to the Excel file</param>
        /// <param name="processId">Process identifier</param>
        void LogExcelFileCreationStart(string fileName, string filePath, string processId);

        /// <summary>
        /// Logs Excel file creation completion
        /// </summary>
        /// <param name="fileName">Name of the Excel file</param>
        /// <param name="elapsed">Time taken to create the file</param>
        /// <param name="rowCount">Number of rows in the file</param>
        /// <param name="processId">Process identifier</param>
        void LogExcelResult(string fileName, TimeSpan elapsed, long rowCount, string processId);

        /// <summary>
        /// Logs Excel file creation completion with full file path
        /// </summary>
        /// <param name="fileName">Name of the Excel file</param>
        /// <param name="filePath">Full path to the Excel file</param>
        /// <param name="elapsed">Time taken to create the file</param>
        /// <param name="rowCount">Number of rows in the file</param>
        /// <param name="processId">Process identifier</param>
        void LogExcelResult(string fileName, string filePath, TimeSpan elapsed, long rowCount, string processId);

        /// <summary>
        /// Logs file save operation
        /// </summary>
        /// <param name="operation">Save operation description</param>
        /// <param name="elapsed">Time taken for the operation</param>
        /// <param name="processId">Process identifier</param>
        void LogFileSave(string operation, TimeSpan elapsed, string processId);

        /// <summary>
        /// Logs file save operation with full file path
        /// </summary>
        /// <param name="operation">Save operation description</param>
        /// <param name="filePath">Full path to the saved file</param>
        /// <param name="elapsed">Time taken for the operation</param>
        /// <param name="processId">Process identifier</param>
        void LogFileSave(string operation, string filePath, TimeSpan elapsed, string processId);

        /// <summary>
        /// Logs skipped operations with reason
        /// </summary>
        /// <param name="fileName">Name of the skipped file</param>
        /// <param name="rowCount">Number of rows that would have been processed</param>
        /// <param name="reason">Reason for skipping</param>
        /// <param name="processId">Process identifier</param>
        void LogSkipped(string fileName, long rowCount, string reason, string processId);

        /// <summary>
        /// Logs skipped operations with reason and full file path
        /// </summary>
        /// <param name="fileName">Name of the skipped file</param>
        /// <param name="filePath">Full path to the skipped file</param>
        /// <param name="rowCount">Number of rows that would have been processed</param>
        /// <param name="reason">Reason for skipping</param>
        /// <param name="processId">Process identifier</param>
        void LogSkipped(string fileName, string filePath, long rowCount, string reason, string processId);

        /// <summary>
        /// Logs processing summary
        /// </summary>
        /// <param name="summary">Processing summary details</param>
        void LogProcessingSummary(ProcessingSummary summary);

        /// <summary>
        /// Creates a timer for tracking operation duration
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <param name="processId">Process identifier</param>
        /// <returns>A disposable timer that logs completion when disposed</returns>
        IOperationTimer StartTimer(string operationName, string processId);
    }
}