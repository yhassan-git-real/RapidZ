using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RapidZ.Core.Database;

namespace RapidZ.Core.Services
{
    public class LogParserService : ILogParserService
    {
        private const string DEFAULT_LOG_FOLDER = "Logs";
        private readonly string _basePath;
        private readonly string _logDirectory;
        
        public LogParserService(string? basePath = null)
        {
            // If no base path is provided, use the application directory
            _basePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory;
            
            // Default log directory (will be overridden if config is available)
            _logDirectory = Path.Combine(_basePath, DEFAULT_LOG_FOLDER);
            
            // Check if F:\RapidZ\Logs exists directly (hardcoded path as fallback)
            string knownLogPath = @"F:\RapidZ\Logs";
            if (Directory.Exists(knownLogPath))
            {
                _logDirectory = knownLogPath;
                System.Diagnostics.Debug.WriteLine($"Using known log directory: {_logDirectory}");
            }
            
            // Try to get the log directory from configuration
            try
            {
                // Use the static ConfigurationCacheService to get database settings
                var dbConfig = ConfigurationCacheService.GetSharedDatabaseSettings();
                if (dbConfig != null && !string.IsNullOrEmpty(dbConfig.LogDirectory))
                {
                    _logDirectory = dbConfig.LogDirectory;
                    System.Diagnostics.Debug.WriteLine($"Using configured log directory: {_logDirectory}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading log directory from config: {ex.Message}");
                // Fall back to default log directory
            }
        }
        
        public async Task<ExecutionSummary> GetLatestExecutionSummaryAsync(string mode = "Export")
        {
            try
            {
                // This IO operation runs on a background thread
                string logFilePath = GetCurrentLogFilePath(mode);
                
                if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Log file not found: {logFilePath}");
                    return ExecutionSummary.Empty;
                }
                
                // Use ConfigureAwait(false) to avoid deadlocks when working with async IO
                string logContent = await File.ReadAllTextAsync(logFilePath).ConfigureAwait(false);
                
                // Parse content on the background thread
                var summary = ParseLogContent(logContent);
                
                if (summary == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse log content");
                    return ExecutionSummary.Empty;
                }
                
                return summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading log file: {ex.Message}");
                return ExecutionSummary.Empty;
            }
        }
        
        private string GetCurrentLogFilePath(string mode)
        {
            try
            {
                string today = DateTime.Now.ToString("yyyyMMdd");
                string logFileName = $"{mode}_Log_{today}.txt";
                
                // List of potential log locations in order of preference
                var possiblePaths = new[]
                {
                    // 1. Configured log directory from settings
                    Path.Combine(_logDirectory, logFileName),
                    
                    // 2. Hardcoded known path for the project
                    Path.Combine(@"F:\RapidZ\Logs", logFileName),
                    
                    // 3. Default location relative to base path
                    Path.Combine(_basePath, DEFAULT_LOG_FOLDER, logFileName),
                    
                    // 4. Parent directory of base path
                    Path.Combine(Path.GetDirectoryName(_basePath) ?? string.Empty, "Logs", logFileName),
                    
                    // 5. Two levels up from base path (common in dev environments)
                    Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_basePath) ?? string.Empty) ?? string.Empty, "Logs", logFileName)
                };
                
                // Check each path in order
                foreach (var path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found log file: {path}");
                        return path;
                    }
                }
                
                // Log all attempted paths for debugging
                System.Diagnostics.Debug.WriteLine($"Log file not found: {logFileName}");
                System.Diagnostics.Debug.WriteLine($"Attempted paths:");
                foreach (var path in possiblePaths.Where(p => !string.IsNullOrEmpty(p)))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {path} (exists: {File.Exists(path)})");
                }
                
                // Return the first path even if it doesn't exist
                return possiblePaths[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding log file: {ex.Message}");
                return string.Empty;
            }
        }
        
        private ExecutionSummary ParseLogContent(string logContent)
        {
            if (string.IsNullOrEmpty(logContent))
            {
                System.Diagnostics.Debug.WriteLine("Log content is empty");
                return ExecutionSummary.Empty;
            }
            
            try
            {
                // Find the last completed process in the logs
                var completedBlocks = Regex.Matches(logContent, @"PROCESS START:.*?PROCESS COMPLETE:.*?Result:.*?\n", 
                    RegexOptions.Singleline);
                
                if (completedBlocks.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No completed process blocks found in log");
                    // Try an alternative pattern that might match
                    completedBlocks = Regex.Matches(logContent, @"\[P\d+\].*?PROCESS START:.*?\[P\d+\].*?PROCESS COMPLETE:.*?\[P\d+\].*?Result:.*?\n",
                        RegexOptions.Singleline);
                        
                    if (completedBlocks.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No process blocks found with alternative pattern either");
                        return ExecutionSummary.Empty;
                    }
                }
                
                // Get the last completed process block
                string lastBlock = completedBlocks[completedBlocks.Count - 1].Value;
                System.Diagnostics.Debug.WriteLine($"Found process block: {lastBlock.Substring(0, Math.Min(100, lastBlock.Length))}...");
                
                // Extract information from this block
                var summary = new ExecutionSummary();
                
                // Extract timestamp - try different patterns
                var timestampMatch = Regex.Match(lastBlock, @"\[([\d-]+ [\d:\.]+)\].*?PROCESS COMPLETE");
                if (timestampMatch.Success)
                {
                    try
                    {
                        summary.TimeStamp = DateTime.Parse(timestampMatch.Groups[1].Value);
                    }
                    catch
                    {
                        // If parsing fails, try another format or leave default
                        System.Diagnostics.Debug.WriteLine("Failed to parse timestamp");
                    }
                }
                
                // Extract row count
                var rowCountMatch = Regex.Match(lastBlock, @"Row count: ([\d,]+)");
                if (rowCountMatch.Success)
                {
                    summary.RowCount = rowCountMatch.Groups[1].Value;
                }
                else
                {
                    // Try alternative pattern - look for records mention
                    var recordsMatch = Regex.Match(lastBlock, @"(\d+) records");
                    if (recordsMatch.Success)
                    {
                        summary.RowCount = recordsMatch.Groups[1].Value;
                    }
                }
                
                // Extract filename
                var fileNameMatch = Regex.Match(lastBlock, @"(Creating Excel file|Excel Complete|file:) ([^\s|\r\n]+\.xlsx)");
                if (fileNameMatch.Success)
                {
                    summary.FileName = fileNameMatch.Groups[2].Value;
                }
                
                // Extract duration
                var durationMatch = Regex.Match(lastBlock, @"Total Time: ([\d:\.]+)");
                if (!durationMatch.Success)
                {
                    durationMatch = Regex.Match(lastBlock, @"⏱️\s*([\d:\.]+)");
                }
                
                if (durationMatch.Success)
                {
                    summary.Duration = durationMatch.Groups[1].Value;
                }
                
                // Extract result
                var resultMatch = Regex.Match(lastBlock, @"Result: ([^\r\n]+)");
                if (resultMatch.Success)
                {
                    string resultText = resultMatch.Groups[1].Value.Trim();
                    
                    if (resultText.StartsWith("Success") || resultText.Contains("✅"))
                    {
                        summary.Result = resultText;
                        summary.Status = ExecutionStatus.Completed;
                    }
                    else if (resultText.Contains("Failed") || resultText.Contains("Error") || resultText.Contains("❌"))
                    {
                        summary.Result = resultText;
                        summary.Status = ExecutionStatus.Failed;
                    }
                    else
                    {
                        summary.Result = resultText;
                        summary.Status = ExecutionStatus.Unknown;
                    }
                }
                else if (lastBlock.Contains("PROCESS COMPLETE") || lastBlock.Contains("✅"))
                {
                    // If no explicit result but process completed
                    summary.Result = "Completed successfully";
                    summary.Status = ExecutionStatus.Completed;
                }
                
                // Extract file path (if available)
                summary.FilePath = GetExportFilePath(summary.FileName);
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Parsed summary: Status={summary.Status}, File={summary.FileName}, Rows={summary.RowCount}, Duration={summary.Duration}");
                
                return summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing log content: {ex.Message}");
                return ExecutionSummary.Empty;
            }
        }
        
        private string GetExportFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;
                
            // Construct the export file path using the known EXPORT_Excel folder
            return Path.Combine(_basePath, "..", "EXPORT_Excel", fileName);
        }
    }
    
    public enum ExecutionStatus
    {
        Idle,
        Processing,
        Completed,
        Failed,
        Unknown
    }
    
    public class ExecutionSummary
    {
        public static ExecutionSummary Empty => new ExecutionSummary
        {
            Status = ExecutionStatus.Idle,
            Result = "No execution data available"
        };
        
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Unknown;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RowCount { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;
        public string Duration { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        
        public bool HasData => !string.IsNullOrEmpty(FileName) || !string.IsNullOrEmpty(Result);
        
        public string FormattedTime => TimeStamp != DateTime.MinValue ? 
            TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
    }
}
