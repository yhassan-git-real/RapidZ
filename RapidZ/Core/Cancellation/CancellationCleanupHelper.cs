using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using RapidZ.Core.Logging.Services;

namespace RapidZ.Core.Cancellation
{
    /// <summary>
    /// Provides safe cleanup operations during cancellation scenarios
    /// Handles disposal of database resources and file cleanup
    /// </summary>
    public static class CancellationCleanupHelper
    {
        private static readonly Lazy<RapidZ.Core.Logging.Abstractions.IModuleLogger> _logger = new Lazy<RapidZ.Core.Logging.Abstractions.IModuleLogger>(() => LoggerFactory.GetCancellationLogger());

        /// <summary>
        /// Safely disposes a SQL connection during cancellation
        /// </summary>
        /// <param name="connection">The connection to dispose</param>
        /// <param name="processId">Process ID for logging</param>
        public static void SafeDisposeConnection(SqlConnection? connection, string? processId = null)
        {
            if (connection == null) return;

            try
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                }
                connection.Dispose();
                _logger.Value.LogInfo($"Database connection disposed successfully", processId);
            }
            catch (Exception ex)
            {
                _logger.Value.LogError($"Error disposing database connection during cancellation: {ex.Message}", ex, processId);
            }
        }

        /// <summary>
        /// Safely disposes a SQL data reader during cancellation
        /// </summary>
        /// <param name="reader">The reader to dispose</param>
        /// <param name="processId">Process ID for logging</param>
        public static void SafeDisposeReader(SqlDataReader? reader, string? processId = null)
        {
            if (reader == null) return;

            try
            {
                if (!reader.IsClosed)
                {
                    reader.Close();
                }
                reader.Dispose();
                _logger.Value.LogInfo($"Database reader disposed successfully", processId);
            }
            catch (Exception ex)
            {
                _logger.Value.LogError($"Error disposing database reader during cancellation: {ex.Message}", ex, processId);
            }
        }

        /// <summary>
        /// Safely cancels a SQL command during cancellation
        /// </summary>
        /// <param name="command">The command to cancel</param>
        /// <param name="processId">Process ID for logging</param>
        public static void SafeCancelCommand(SqlCommand? command, string? processId = null)
        {
            if (command == null) return;

            try
            {
                command.Cancel();
                _logger.Value.LogInfo($"Database command cancelled successfully", processId);
            }
            catch (Exception ex)
            {
                _logger.Value.LogError($"Error cancelling database command: {ex.Message}", ex, processId);
            }
        }

        /// <summary>
        /// Safely deletes a partially created file during cancellation
        /// </summary>
        /// <param name="filePath">Path to the file to delete</param>
        /// <param name="processId">Process ID for logging</param>
        /// <returns>True if file was deleted successfully</returns>
        public static bool SafeDeletePartialFile(string? filePath, string? processId = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.Value.LogInfo($"Partially created file deleted during cancellation: {Path.GetFileName(filePath)}", processId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Value.LogError($"Error deleting partial file during cancellation ({Path.GetFileName(filePath)}): {ex.Message}", ex, processId);
                return false;
            }
        }

        /// <summary>
        /// Moves a partially created file to a temporary location during cancellation
        /// </summary>
        /// <param name="filePath">Path to the file to move</param>
        /// <param name="tempDirectory">Temporary directory path (optional)</param>
        /// <param name="processId">Process ID for logging</param>
        /// <returns>Path to moved file if successful, null otherwise</returns>
        public static string? SafeMovePartialFileToTemp(string? filePath, string? tempDirectory = null, string? processId = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null;

            try
            {
                var fileName = Path.GetFileName(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var tempFileName = $"CANCELLED_{timestamp}_{fileName}";
                
                var tempDir = tempDirectory ?? Path.Combine(Path.GetTempPath(), "RapidZ_Cancelled");
                Directory.CreateDirectory(tempDir);
                
                var tempFilePath = Path.Combine(tempDir, tempFileName);
                File.Move(filePath, tempFilePath);
                
                _logger.Value.LogInfo($"Partial file moved to temp location during cancellation: {tempFileName}", processId);
                return tempFilePath;
            }
            catch (Exception ex)
            {
                _logger.Value.LogError($"Error moving partial file to temp during cancellation ({Path.GetFileName(filePath)}): {ex.Message}", ex, processId);
                return null;
            }
        }

        /// <summary>
        /// Cleans up multiple resources in a safe manner during cancellation
        /// </summary>
        /// <param name="cleanup">Cleanup configuration</param>
        public static async Task SafeCleanupResources(CancellationCleanupConfig cleanup)
        {
            var processId = cleanup.ProcessId ?? _logger.Value.GenerateProcessId();
            _logger.Value.LogProcessStart("Cancellation Cleanup", "Cleaning up resources after operation cancellation", processId);

            var tasks = new List<Task>();

            // Database cleanup
            if (cleanup.Connection != null || cleanup.Reader != null || cleanup.Command != null)
            {
                tasks.Add(Task.Run(() =>
                {
                    SafeCancelCommand(cleanup.Command, processId);
                    SafeDisposeReader(cleanup.Reader, processId);
                    SafeDisposeConnection(cleanup.Connection, processId);
                }));
            }

            // File cleanup
            if (cleanup.PartialFiles?.Count > 0)
            {
                tasks.Add(Task.Run(() =>
                {
                    foreach (var filePath in cleanup.PartialFiles)
                    {
                        if (cleanup.MoveToTempInsteadOfDelete)
                        {
                            SafeMovePartialFileToTemp(filePath, cleanup.TempDirectory, processId);
                        }
                        else
                        {
                            SafeDeletePartialFile(filePath, processId);
                        }
                    }
                }));
            }

            // Custom cleanup actions
            if (cleanup.CustomCleanupActions?.Count > 0)
            {
                foreach (var action in cleanup.CustomCleanupActions)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            _logger.Value.LogError($"Error in custom cleanup action: {ex.Message}", ex, processId);
                        }
                    }));
                }
            }

            // Wait for all cleanup tasks to complete
            await Task.WhenAll(tasks);

            _logger.Value.LogProcessComplete("Cancellation Cleanup", TimeSpan.Zero, "All resources cleaned up", processId);
        }
    }

    /// <summary>
    /// Configuration for cancellation cleanup operations
    /// </summary>
    public class CancellationCleanupConfig
    {
        /// <summary>
        /// Process ID for logging purposes
        /// </summary>
        public string? ProcessId { get; set; }

        /// <summary>
        /// SQL connection to dispose
        /// </summary>
        public SqlConnection? Connection { get; set; }

        /// <summary>
        /// SQL data reader to dispose
        /// </summary>
        public SqlDataReader? Reader { get; set; }

        /// <summary>
        /// SQL command to cancel
        /// </summary>
        public SqlCommand? Command { get; set; }

        /// <summary>
        /// List of partial files to clean up
        /// </summary>
        public List<string>? PartialFiles { get; set; }

        /// <summary>
        /// Whether to move files to temp instead of deleting them
        /// </summary>
        public bool MoveToTempInsteadOfDelete { get; set; } = false;

        /// <summary>
        /// Custom temporary directory for moved files
        /// </summary>
        public string? TempDirectory { get; set; }

        /// <summary>
        /// Custom cleanup actions to execute
        /// </summary>
        public List<Action>? CustomCleanupActions { get; set; }
    }
}