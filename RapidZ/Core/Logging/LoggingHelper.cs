using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RapidZ.Core.Parameters.Export;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace RapidZ.Core.Logging
{
    public sealed class LoggingHelper : IDisposable
    {
        private static readonly Lazy<LoggingHelper> _instance = new Lazy<LoggingHelper>(() => new LoggingHelper());
        public static LoggingHelper Instance => _instance.Value;

        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
        private readonly string _logDirectory;
        private string _logFilePrefix;
        private string _logFileExtension;
        private readonly int _flushIntervalSeconds;
        private string _currentLogFile = string.Empty;
        private DateTime _currentLogDate = DateTime.MinValue;
        private bool _disposed = false;
        
        // Performance optimization: Cache DateTime.Now for reduced syscalls
        private DateTime _lastTimestampCache = DateTime.Now;
        private long _lastTickCount = Environment.TickCount64;

        public enum LogLevel
        {
            INFO,
            WARNING,
            ERROR
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? StackTrace { get; set; }
            public string? ProcessId { get; set; }
        }

        private static int _processCounter = 0;

        private LoggingHelper()
        {
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("Config/database.appsettings.json", optional: false);

            var cfg = builder.Build();

            _logDirectory = cfg["DatabaseConfig:LogDirectory"] ?? Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(_logDirectory);

            // Default prefix/extension before any module sets them
            _logFilePrefix = "AppLog";
            _logFileExtension = ".txt";
            _flushIntervalSeconds = 1; // fixed default; can be externalized later if needed

            UpdateLogFileName();
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null,
                TimeSpan.FromSeconds(_flushIntervalSeconds), TimeSpan.FromSeconds(_flushIntervalSeconds));
        }

        // Allow modules (export/import) to set their logging file naming dynamically
        public void SetModuleLogFile(string prefix, string? extension = null)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return;
            _logFilePrefix = prefix.Trim();
            if (!string.IsNullOrWhiteSpace(extension))
            {
                _logFileExtension = extension!.StartsWith('.') ? extension : "." + extension;
            }
            UpdateLogFileName();
        }

        public string GenerateProcessId()
        {
            return $"P{Interlocked.Increment(ref _processCounter):D4}";
        }

        // Performance optimization: Get timestamp with reduced DateTime.Now calls
        private DateTime GetOptimizedTimestamp()
        {
            var currentTicks = Environment.TickCount64;
            // Only call DateTime.Now if more than 100ms have passed
            if (currentTicks - _lastTickCount > 100)
            {
                _lastTimestampCache = DateTime.Now;
                _lastTickCount = currentTicks;
            }
            return _lastTimestampCache.AddMilliseconds(currentTicks - _lastTickCount);
        }

        private void UpdateLogFileName()
        {
            var today = _lastTimestampCache.Date; // Use cached timestamp instead of DateTime.Now
            if (_currentLogDate != today)
            {
                _currentLogDate = today;
                _currentLogFile = Path.Combine(_logDirectory, $"{_logFilePrefix}_{today:yyyyMMdd}{_logFileExtension}");
            }
        }

        public void LogInfo(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.INFO, message, null, processId);
        }

        public void LogWarning(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.WARNING, message, null, processId);
        }

        public void LogError(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.ERROR, message, null, processId);
        }

        public void LogError(string message, Exception ex, string? processId = null)
        {
            var errorMessage = $"{message} - {ex.Message}";
            EnqueueLog(LogLevel.ERROR, errorMessage, ex.StackTrace, processId);
        }

        public void LogProcessStart(string processName, string parameters, string processId)
        {
            EnqueueLog(LogLevel.INFO, new string('=', 100), null, null);
            EnqueueLog(LogLevel.INFO, $"🚀 PROCESS START: {processName}", null, processId);
            EnqueueLog(LogLevel.INFO, $"📋 Parameters: {parameters}", null, processId);
            EnqueueLog(LogLevel.INFO, new string('-', 100), null, null);
        }

        public void LogProcessComplete(string processName, TimeSpan elapsed, string result, string processId)
        {
            EnqueueLog(LogLevel.INFO, new string('-', 100), null, null);
            EnqueueLog(LogLevel.INFO, $"✅ PROCESS COMPLETE: {processName}", null, processId);
            EnqueueLog(LogLevel.INFO, $"⏱️  Total Time: {elapsed:mm\\:ss\\.fff}", null, processId);
            EnqueueLog(LogLevel.INFO, $"📊 Result: {result}", null, processId);
            EnqueueLog(LogLevel.INFO, new string('=', 100), null, null);
        }

        public void LogStep(string stepName, string details, string processId)
        {
            EnqueueLog(LogLevel.INFO, $"  ➤ {stepName}: {details}", null, processId);
        }

        public void LogDetailedParameters(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string forCount, string forName, string port, string processId)
        {
            string parameters = ExportParameterHelper.FormatStoredProcedureParameters(fromMonth, toMonth, hsCode, product, iec, exporter, forCount, forName, port);
            LogStep("Parameters Detail", parameters, processId);
        }

        public void LogStoredProcedure(string spName, string parameters, TimeSpan elapsed, string processId)
        {
            LogStep("SP Executed", $"{spName} | {parameters} | Time: {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogDataReader(string viewName, string orderBy, long rowCount, string processId)
        {
            LogStep("Data Reader", $"View: {viewName} | Order: {orderBy} | Rows: {rowCount:N0}", processId);
        }

        public void LogExcelFileCreationStart(string fileName, string processId)
        {
            LogStep("Excel Creation", $"Starting file: {fileName}", processId);
        }

        public void LogExcelResult(string fileName, TimeSpan elapsed, long rowCount, string processId)
        {
            LogStep("Excel Generated", $"{fileName} | Rows: {rowCount:N0} | Time: {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogFileSave(string operation, TimeSpan elapsed, string processId)
        {
            LogStep("File Save", $"{operation} | Time: {elapsed:mm\\:ss\\.fff}", processId);
        }

        public void LogSkipped(string fileName, long rowCount, string reason, string processId)
        {
            LogWarning($"⚠️  SKIPPED: {fileName} | Rows: {rowCount:N0} | Reason: {reason}", processId);
        }

        public void LogProcessingSummary(int totalCombinations, int filesGenerated, int combinationsSkipped, TimeSpan totalElapsed)
        {
            var summary = new StringBuilder();
            summary.AppendLine("PROCESSING SUMMARY");
            summary.AppendLine($"Total Combinations: {totalCombinations}");
            summary.AppendLine($"Files Generated: {filesGenerated}");
            summary.AppendLine($"Combinations Skipped: {combinationsSkipped}");
            summary.AppendLine($"Success Rate: {((double)filesGenerated / totalCombinations * 100):F1}%");
            summary.AppendLine($"Total Processing Time: {totalElapsed:hh\\:mm\\:ss}");
            
            LogInfo(summary.ToString());
        }

        private void EnqueueLog(LogLevel level, string message, string? stackTrace, string? processId)
        {
            if (_disposed) return;

            _logQueue.Enqueue(new LogEntry
            {
                Timestamp = GetOptimizedTimestamp(),
                Level = level,
                Message = message,
                StackTrace = stackTrace,
                ProcessId = processId
            });
        }

        private async Task FlushLogsAsync()
        {
            if (_disposed || !_flushSemaphore.Wait(0)) return;

            try
            {
                UpdateLogFileName();
                
                var logEntries = new List<LogEntry>();
                while (_logQueue.TryDequeue(out var entry))
                {
                    logEntries.Add(entry);
                }

                if (logEntries.Count == 0) return;

                var logContent = new StringBuilder();
                foreach (var entry in logEntries)
                {
                    var processIdPart = string.IsNullOrEmpty(entry.ProcessId) ? "" : $" [{entry.ProcessId}]";
                    logContent.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.Level}{processIdPart} {entry.Message}");
                    
                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        logContent.AppendLine($"Stack Trace: {entry.StackTrace}");
                    }
                }

                await File.AppendAllTextAsync(_currentLogFile, logContent.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logging failed: {ex.Message}");
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _flushTimer?.Dispose();
            
            // Final flush
            FlushLogsAsync().Wait(TimeSpan.FromSeconds(5));
            
            _flushSemaphore?.Dispose();
        }
    }
}