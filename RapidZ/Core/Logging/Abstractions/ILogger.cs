using System;
using System.Threading.Tasks;

namespace RapidZ.Core.Logging.Abstractions
{
    /// <summary>
    /// Core logging interface that defines the contract for all loggers in the system
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Generates a unique process identifier for tracking related log entries
        /// </summary>
        /// <returns>A unique process identifier</returns>
        string GenerateProcessId();

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="processId">Optional process identifier for correlation</param>
        void LogInfo(string message, string? processId = null);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="processId">Optional process identifier for correlation</param>
        void LogWarning(string message, string? processId = null);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="processId">Optional process identifier for correlation</param>
        void LogError(string message, string? processId = null);

        /// <summary>
        /// Logs an error message with exception details
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="processId">Optional process identifier for correlation</param>
        void LogError(string message, Exception ex, string? processId = null);

        /// <summary>
        /// Forces immediate flush of pending log entries
        /// </summary>
        /// <returns>A task representing the flush operation</returns>
        Task FlushAsync();
    }
}