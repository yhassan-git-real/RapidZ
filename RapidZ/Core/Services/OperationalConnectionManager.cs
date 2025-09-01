using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using RapidZ.Core.Database;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RapidZ.Core.Services
{
    /// <summary>
    /// Manages database connections for operational use with connect-execute-disconnect pattern
    /// </summary>
    public class OperationalConnectionManager : IDisposable
    {
        private readonly SharedDatabaseSettings _dbSettings;
        private readonly DatabaseConnectionService _connectionService;
        private SqlConnection? _activeConnection;
        private bool _isConnected = false;

        public OperationalConnectionManager()
        {
            _dbSettings = LoadDatabaseSettings();
            _connectionService = DatabaseConnectionService.Instance;
        }

        /// <summary>
        /// Establishes a database connection for operational use
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_isConnected && _activeConnection?.State == System.Data.ConnectionState.Open)
                {
                    return true; // Already connected
                }

                // Dispose existing connection if any
                await DisconnectAsync();

                // Create new connection
                _activeConnection = new SqlConnection(_dbSettings.ConnectionString);
                await _activeConnection.OpenAsync();
                _isConnected = true;

                // Update UI status to show connected
                _connectionService.SetOperationalConnected();

                return true;
            }
            catch (Exception)
            {
                _isConnected = false;
                _activeConnection?.Dispose();
                _activeConnection = null;

                // Update UI status to show disconnected
                _connectionService.SetOperationalDisconnected();

                return false;
            }
        }

        /// <summary>
        /// Disconnects from the database and updates UI status
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_activeConnection != null)
                {
                    if (_activeConnection.State == System.Data.ConnectionState.Open)
                    {
                        await _activeConnection.CloseAsync();
                    }
                    _activeConnection.Dispose();
                    _activeConnection = null;
                }
            }
            catch (Exception)
            {
                // Ignore disposal errors
            }
            finally
            {
                _isConnected = false;
                // Update UI status to show disconnected
                _connectionService.SetOperationalDisconnected();
            }
        }

        /// <summary>
        /// Gets the active connection for database operations
        /// </summary>
        /// <returns>Active SqlConnection or null if not connected</returns>
        public SqlConnection? GetConnection()
        {
            return _isConnected && _activeConnection?.State == System.Data.ConnectionState.Open 
                ? _activeConnection 
                : null;
        }

        /// <summary>
        /// Executes a database operation with automatic connection management
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">Database operation to execute</param>
        /// <returns>Result of the operation</returns>
        public async Task<T> ExecuteWithConnectionAsync<T>(Func<SqlConnection, Task<T>> operation)
        {
            var wasConnected = _isConnected;
            
            try
            {
                // Connect if not already connected
                if (!await ConnectAsync())
                {
                    throw new InvalidOperationException("Failed to establish database connection");
                }

                // Execute the operation
                var connection = GetConnection();
                if (connection == null)
                {
                    throw new InvalidOperationException("No active database connection available");
                }

                return await operation(connection);
            }
            finally
            {
                // Disconnect if we established the connection in this call
                if (!wasConnected)
                {
                    await DisconnectAsync();
                }
            }
        }

        /// <summary>
        /// Executes a database operation with automatic connection management (void return)
        /// </summary>
        /// <param name="operation">Database operation to execute</param>
        public async Task ExecuteWithConnectionAsync(Func<SqlConnection, Task> operation)
        {
            await ExecuteWithConnectionAsync<object?>(async connection =>
            {
                await operation(connection);
                return null;
            });
        }

        /// <summary>
        /// Checks if currently connected to the database
        /// </summary>
        public bool IsConnected => _isConnected && _activeConnection?.State == System.Data.ConnectionState.Open;

        private SharedDatabaseSettings LoadDatabaseSettings()
        {
            try
            {
                const string json = "Config/database.appsettings.json";
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(json, false);
                var cfg = builder.Build();
                var root = cfg.Get<SharedDatabaseSettingsRoot>() 
                    ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
                return root.DatabaseConfig;
            }
            catch (Exception)
            {
                // Return default settings if config fails to load
                return new SharedDatabaseSettings
                {
                    ConnectionString = "Server=localhost;Database=Unknown;",
                    LogDirectory = "Logs"
                };
            }
        }

        public void Dispose()
        {
            // Use synchronous disconnect for disposal
            try
            {
                if (_activeConnection != null)
                {
                    if (_activeConnection.State == System.Data.ConnectionState.Open)
                    {
                        _activeConnection.Close();
                    }
                    _activeConnection.Dispose();
                    _activeConnection = null;
                }
            }
            catch (Exception)
            {
                // Ignore disposal errors
            }
            finally
            {
                _isConnected = false;
                _connectionService.SetOperationalDisconnected();
            }
        }
    }
}