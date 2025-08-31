using System;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.Data.SqlClient;
using RapidZ.Core.Logging.Services;
using RapidZ.Core.Helpers;
using RapidZ.Core.Database;
using RapidZ.Core.Services;
using RapidZ.Core.Cancellation;
using RapidZ.Features.Import;

namespace RapidZ.Core.DataAccess
{
    public class ImportDataAccess
    {
        private readonly RapidZ.Core.Logging.Abstractions.IModuleLogger _logger;
        private readonly ImportSettings _settings;
        private readonly SharedDatabaseSettings _dbSettings;

        public ImportDataAccess(ImportSettings settings)
        {
            _logger = LoggerFactory.GetImportLogger();
            _settings = settings;
            // Use static configuration cache methods like TradeDataHub
            _dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
        }

        public Tuple<SqlConnection, SqlDataReader, long> GetDataReader(
            string fromMonth, string toMonth, string hsCode, string product, string iec, string importer, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
        {
            SqlConnection? con = null;
            SqlDataReader? reader = null;
            SqlCommand? currentCommand = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                con = new SqlConnection(_dbSettings.ConnectionString);
                con.Open();

                cancellationToken.ThrowIfCancellationRequested();

                // Determine effective view, stored procedure, and order by column
                string effectiveStoredProcedureName = storedProcedureName ?? _settings.Database.StoredProcedureName;
                string effectiveViewName = viewName ?? _settings.Database.ViewName;
                string effectiveOrderByColumn = _settings.Database.OrderByColumn;
                
                // If using a custom view from ImportObjects, get its OrderByColumn
                if (viewName != null && _settings.ImportObjects != null)
                {
                    var customView = _settings.ImportObjects.Views?.FirstOrDefault(v => v.Name == viewName);
                    if (customView != null && !string.IsNullOrEmpty(customView.OrderByColumn))
                    {
                        effectiveOrderByColumn = customView.OrderByColumn;
                    }
                }
                
                // Get current process ID if available
                string? processId = Thread.CurrentThread.Name?.StartsWith("P") == true ? 
                    Thread.CurrentThread.Name : null;
                
                // Add detailed logging with proper formatting
                if (!string.IsNullOrEmpty(processId))
                {
                    _logger.LogStep("Database", $"Connecting to {_dbSettings.ConnectionString.Split(';').FirstOrDefault()}", processId);
                    _logger.LogStep("Database", $"Executing SP: {effectiveStoredProcedureName} on View: {effectiveViewName}", processId);
                    _logger.LogStep("Parameters", $"fromMonth={fromMonth}, ToMonth={toMonth}, hs={hsCode}, prod={product}, ImpCmp={importer}, forcount={country}, forname={name}, port={port}", processId);
                }
                
                // Execute stored procedure using parameterized query for better performance and security
                using (var cmd = new SqlCommand(effectiveStoredProcedureName, con))
                {
                    currentCommand = cmd;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

                    // Add parameters with correct names matching the stored procedure - use the exact names expected by the SP
                    cmd.Parameters.AddWithValue("@fromMonth", fromMonth);
                    cmd.Parameters.AddWithValue("@ToMonth", toMonth);
                    cmd.Parameters.AddWithValue("@hs", hsCode);        // This is what the SP expects based on error
                    cmd.Parameters.AddWithValue("@prod", product);     // Keeping naming consistent with original SP
                    cmd.Parameters.AddWithValue("@Iec", iec);
                    cmd.Parameters.AddWithValue("@ImpCmp", importer);  // Using original parameter name from SP
                    cmd.Parameters.AddWithValue("@forcount", country); // Using original parameter name
                    cmd.Parameters.AddWithValue("@forname", name);     // Using original parameter name
                    cmd.Parameters.AddWithValue("@port", port);

                    // Register cancellation callback to cancel the command
                    using var registration = cancellationToken.Register(() => 
                    {
                        CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                    });

                    cmd.ExecuteNonQuery();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                currentCommand = null; // Command completed successfully

                // Row count
                long recordCount = 0;
                using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {effectiveViewName}", con))
                {
                    currentCommand = countCmd;
                    countCmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

                    using var registration = cancellationToken.Register(() => 
                    {
                        CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                    });

                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Open streaming reader
                var dataCmd = new SqlCommand($"SELECT * FROM {effectiveViewName} ORDER BY [{effectiveOrderByColumn}]", con);
                currentCommand = dataCmd;
                dataCmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

                using var dataRegistration = cancellationToken.Register(() => 
                {
                    CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                });

                reader = dataCmd.ExecuteReader();
                cancellationToken.ThrowIfCancellationRequested();

                return new Tuple<SqlConnection, SqlDataReader, long>(con, reader, recordCount);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                CancellationCleanupHelper.SafeDisposeReader(reader);
                CancellationCleanupHelper.SafeDisposeConnection(con);
                throw;
            }
            catch
            {
                reader?.Dispose();
                con?.Dispose();
                throw;
            }
        }
    }
}