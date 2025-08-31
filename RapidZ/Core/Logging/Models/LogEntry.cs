using System;
using RapidZ.Core.Logging.Enums;

namespace RapidZ.Core.Logging.Models
{
    /// <summary>
    /// Represents a single log entry with all necessary metadata
    /// </summary>
    internal sealed class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp when the log entry was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the log entry
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the log message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stack trace for error entries
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the process identifier for correlation
        /// </summary>
        public string? ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the module name that generated this log entry
        /// </summary>
        public string? ModuleName { get; set; }
    }
}