using System;

namespace RapidZ.Features.Monitoring.Models
{
    public enum StatusType
    {
        Idle,
        Running,
        Completed,
        Error,
        Cancelled
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public class MonitoringLog
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class StatusInfo
    {
        public StatusType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
