using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Logging.Abstractions;
using RapidZ.Core.Logging.Enums;
using RapidZ.Core.Logging.Models;

namespace RapidZ.Core.Logging.Core
{
    /// <summary>
    /// Base implementation of logging functionality with optimized performance and async flushing
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
        private readonly string _logDirectory;
        private readonly string _logFileExtension;
        private readonly int _flushIntervalSeconds;
        private string _currentLogFile = string.Empty;
        private DateTime _currentLogDate = DateTime.MinValue;
        private bool _disposed = false;
        
        // Performance optimization: Cache DateTime.Now for reduced syscalls
        private DateTime _lastTimestampCache = DateTime.Now;
        private long _lastTickCount = Environment.TickCount64;
        
        private static int _processCounter = 0;

        /// <summary>
        /// Gets the file prefix for log files
        /// </summary>
        protected abstract string LogFilePrefix { get; }

        /// <summary>
        /// Gets the module name for this logger
        /// </summary>
        protected abstract string ModuleName { get; }

        protected BaseLogger(string? logFileExtension = null)
        {
            _logFileExtension = string.IsNullOrWhiteSpace(logFileExtension) ? ".txt" : 
                (logFileExtension.StartsWith('.') ? logFileExtension : "." + logFileExtension);
            
            // Load shared database config for log directory
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder().SetBasePath(basePath)
                .AddJsonFile("Config/database.appsettings.json", optional: false);
            var cfg = builder.Build();
            
            _logDirectory = cfg["DatabaseConfig:LogDirectory"] ?? Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(_logDirectory);
            
            _flushIntervalSeconds = 1; // Fixed interval for consistent performance
            
            // Don't call UpdateLogFileName() here - let derived class call it after initialization
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null,
                TimeSpan.FromSeconds(_flushIntervalSeconds), TimeSpan.FromSeconds(_flushIntervalSeconds));
        }

        public string GenerateProcessId()
        {
            return $"P{Interlocked.Increment(ref _processCounter):D4}";
        }

        public void LogInfo(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.Info, message, null, processId);
        }

        public void LogWarning(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.Warning, message, null, processId);
        }

        public void LogError(string message, string? processId = null)
        {
            EnqueueLog(LogLevel.Error, message, null, processId);
        }

        public void LogError(string message, Exception ex, string? processId = null)
        {
            var errorMessage = $"{message} - {ex.Message}";
            EnqueueLog(LogLevel.Error, errorMessage, ex.StackTrace, processId);
        }

        public async Task FlushAsync()
        {
            await FlushLogsAsync();
        }

        /// <summary>
        /// Performance optimization: Get timestamp with reduced DateTime.Now calls
        /// </summary>
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

        protected void UpdateLogFileName()
        {
            var today = _lastTimestampCache.Date; // Use cached timestamp instead of DateTime.Now
            if (_currentLogDate != today)
            {
                _currentLogDate = today;
                _currentLogFile = Path.Combine(_logDirectory, $"{LogFilePrefix}_{today:yyyyMMdd}{_logFileExtension}");
            }
        }

        protected void EnqueueLog(LogLevel level, string message, string? stackTrace, string? processId)
        {
            if (_disposed) return;

            _logQueue.Enqueue(new LogEntry
            {
                Timestamp = GetOptimizedTimestamp(),
                Level = level,
                Message = message,
                StackTrace = stackTrace,
                ProcessId = processId,
                ModuleName = ModuleName
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
                    logContent.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.Level.ToString().ToUpper()}{processIdPart} {entry.Message}");
                    
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