using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RapidZ.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;

namespace RapidZ.Services;

// Handles database operations for export data
public class DatabaseService : INotifyPropertyChanged
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly ConfigurationService _configService;
    private bool _isConnected;
    private string _connectionStatus;
    private readonly System.Timers.Timer _connectionCheckTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DatabaseService(ILogger<DatabaseService> logger, ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
        _connectionStatus = "Checking connection...";
        
        // Initialize timer to check connection status every 30 seconds
        _connectionCheckTimer = new System.Timers.Timer(30000);
        _connectionCheckTimer.Elapsed += async (s, e) => await CheckConnectionAsync();
        _connectionCheckTimer.AutoReset = true;
        _connectionCheckTimer.Start();
        
        // Check connection initially
        Task.Run(async () => await CheckConnectionAsync());
    }
    
    // Creates a new SQL connection
    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_configService.AppSettings.Database.ConnectionString);
    }
    
    // Gets database info from connection string
    public ConnectionInfo GetConnectionInfo()
    {
        var connectionString = _configService.AppSettings.Database.ConnectionString;
        var builder = new SqlConnectionStringBuilder(connectionString);
        
        return new ConnectionInfo
        {
            ServerName = builder.DataSource,
            DatabaseName = builder.InitialCatalog,
            UserName = builder.UserID,
            IsConnected = IsConnected
        };
    }
    
    // Check database connection status
    public async Task CheckConnectionAsync()
    {
        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            IsConnected = true;
            ConnectionStatus = "Connected";
            _logger.LogInformation("Database connection successful");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Disconnected";
            _logger.LogError(ex, "Database connection failed");
        }
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set
        {
            if (_connectionStatus != value)
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // Executes a query and returns export data based on filter criteria
    public async Task<List<ExportData>> ExecuteExportDataQuery(ExportDataFilter filter)
    {
        var result = new List<ExportData>();
        
        try
        {
            // First, execute the stored procedure to prepare the data
            string procedureName = _configService.AppSettings.StoredProcedures.ExportData;
            
            // Execute the stored procedure (this updates the database but doesn't return results)
            await ExecuteExportStoredProcedureAsync(procedureName, filter);
            
            // Now get the data from the view
            using var reader = await GetExportDataAsync();
            
            while (await reader.ReadAsync())
            {
                result.Add(MapReaderToExportData(reader));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing export data query");
            throw;
        }
    }
    
    // Map data reader to ExportData object
    private ExportData MapReaderToExportData(SqlDataReader reader)
    {
        return new ExportData
        {
            HSCode = reader["HS_CODE"] is DBNull ? string.Empty : reader["HS_CODE"].ToString(),
            Product = reader["PRODUCT_DESCRIPTION"] is DBNull ? string.Empty : reader["PRODUCT_DESCRIPTION"].ToString(),
            Exporter = reader["EXPORTER"] is DBNull ? string.Empty : reader["EXPORTER"].ToString(),
            IEC = reader["IEC"] is DBNull ? string.Empty : reader["IEC"].ToString(),
            ForeignParty = reader["FOREIGN_PARTY"] is DBNull ? string.Empty : reader["FOREIGN_PARTY"].ToString(),
            ForeignCountry = reader["FOREIGN_COUNTRY"] is DBNull ? string.Empty : reader["FOREIGN_COUNTRY"].ToString(),
            Port = reader["PORT"] is DBNull ? string.Empty : reader["PORT"].ToString(),
            SBDate = reader["SB_DATE"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["SB_DATE"]),
            Mode = reader["MODE"] is DBNull ? string.Empty : reader["MODE"].ToString()
        };
    }
    
    // Executes the export stored procedure with filter parameters (no results returned)
    public async Task ExecuteExportStoredProcedureAsync(string procedureName, ExportDataFilter filter)
    {
        try
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            command.CommandTimeout = _configService.AppSettings.Database.CommandTimeout;
            
            // Add parameters based on the filter
            command.Parameters.AddWithValue("@FromMonth", filter.FromMonth);
            command.Parameters.AddWithValue("@ToMonth", filter.ToMonth);
            command.Parameters.AddWithValue("@HSCode", filter.HSCode);
            command.Parameters.AddWithValue("@Product", filter.Product);
            command.Parameters.AddWithValue("@IEC", filter.IEC);
            command.Parameters.AddWithValue("@Exporter", filter.Exporter);
            command.Parameters.AddWithValue("@ForeignCountry", filter.ForeignCountry);
            command.Parameters.AddWithValue("@ForeignParty", filter.ForeignParty);
            command.Parameters.AddWithValue("@Port", filter.Port);
            command.Parameters.AddWithValue("@Mode", filter.Mode);
            
            // Execute without returning results
            await command.ExecuteNonQueryAsync();
            
            // Close the connection
            await connection.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing export stored procedure");
            throw;
        }
    }
    
    // Executes the export query with filter parameters
    public async Task<SqlDataReader> ExecuteExportQueryAsync(string procedureName, ExportDataFilter filter)
    {
        try
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            command.CommandTimeout = _configService.AppSettings.Database.CommandTimeout;
            
            // Add parameters based on the filter
            command.Parameters.AddWithValue("@FromMonth", filter.FromMonth);
            command.Parameters.AddWithValue("@ToMonth", filter.ToMonth);
            command.Parameters.AddWithValue("@HSCode", filter.HSCode);
            command.Parameters.AddWithValue("@Product", filter.Product);
            command.Parameters.AddWithValue("@IEC", filter.IEC);
            command.Parameters.AddWithValue("@Exporter", filter.Exporter);
            command.Parameters.AddWithValue("@ForeignCountry", filter.ForeignCountry);
            command.Parameters.AddWithValue("@ForeignParty", filter.ForeignParty);
            command.Parameters.AddWithValue("@Port", filter.Port);
            command.Parameters.AddWithValue("@Mode", filter.Mode);
            
            // Return reader (will close connection when reader is closed)
            return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing export query");
            throw;
        }
    }
    
    // Gets export data from the configured view
    public async Task<SqlDataReader> GetExportDataAsync()
    {
        try
        {
            var connection = CreateConnection();
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            // Use order by clause from configuration
            command.CommandText = $"SELECT * FROM {_configService.AppSettings.Views.ExportDataView} {_configService.AppSettings.ApplicationSettings.OrderByClause}";
            command.CommandTimeout = _configService.AppSettings.Database.CommandTimeout;
            
            return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving export data");
            throw;
        }
    }
}
