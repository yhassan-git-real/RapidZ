using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RapidZ.Core.Models;
using RapidZ.Core.DataAccess;
using RapidZ.Core.Services;
using RapidZ.Views.Models;
using RapidZ.Features.Import;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using RapidZ.Core.Parameters;

namespace RapidZ.Core.Services;

// Handles database operations for export data
public class DatabaseService : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly ConfigurationService _configService;
    private readonly ExportDataAccess _exportDataAccess;
    private readonly ImportDataAccess _importDataAccess;
    private readonly DatabaseConnectionService _connectionService;
    private readonly OperationalConnectionManager _operationalConnectionManager;
    private bool _isConnected;
    private string _connectionStatus;

    // PropertyChanged event for UI binding
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnConnectionServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DatabaseConnectionService.ConnectionInfo))
        {
            var connInfo = _connectionService.ConnectionInfo;
            IsConnected = connInfo.ConnectionStatus == "Connected";
            ConnectionStatus = connInfo.ConnectionStatus;
        }
    }



    public DatabaseService(ILogger<DatabaseService> logger, ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
        _exportDataAccess = new ExportDataAccess();
        _connectionService = DatabaseConnectionService.Instance;
        _operationalConnectionManager = new OperationalConnectionManager();
        
        // Subscribe to connection service property changes
        _connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        
        // Use cached import settings from configuration
        var importSettings = ConfigurationCacheService.GetImportSettings();
        _importDataAccess = new ImportDataAccess(importSettings);
        _connectionStatus = "Disconnected";
        _isConnected = false;
    }
    
    // Gets database info from connection string
    public ConnectionInfo GetConnectionInfo()
    {
        var connInfo = _connectionService.ConnectionInfo;
        
        // Get the real response time from the connection service
        string responseTime = connInfo.ResponseTime.ToString();
        
        // Create appropriate status message based on connection status
        string statusMessage;
        if (connInfo.ConnectionStatus == "Connected")
        {
            statusMessage = $"Connected to {connInfo.DatabaseName}";
        }
        else if (connInfo.ConnectionStatus == "Checking...")
        {
            statusMessage = "Checking connection...";
        }
        else
        {
            statusMessage = $"Status: {connInfo.ConnectionStatus}";
        }
        
        return new ConnectionInfo
        {
            ServerName = connInfo.ServerName,
            DatabaseName = connInfo.DatabaseName,
            UserName = connInfo.UserAccount,
            IsConnected = connInfo.ConnectionStatus == "Connected",
            ResponseTime = responseTime,
            LastConnectionTime = connInfo.LastChecked,
            StatusMessage = statusMessage
        };
    }
    
    // Gets detailed database connection info from the connection service
    public DatabaseConnectionInfo GetDetailedConnectionInfo()
    {
        return _connectionService.ConnectionInfo;
    }
    
    // Check database connection status (for compatibility)
    public async Task CheckConnectionAsync()
    {
        try
        {
            // Update local connection status from the connection service
            var connInfo = _connectionService.ConnectionInfo;
            IsConnected = connInfo.ConnectionStatus == "Connected";
            ConnectionStatus = connInfo.ConnectionStatus;
            
            // Raise property changed to refresh the UI with the latest connection info
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(ConnectionStatus));
            
            _logger.LogInformation("Database connection status updated");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Connection failed";
            
            // Raise property changed for the failure case too
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(ConnectionStatus));
            
            _logger.LogError(ex, "Database connection status update failed");
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
    
    // Executes the export query with filter parameters
    public (SqlConnection connection, SqlDataReader reader, long recordCount) ExecuteExportQuery(ExportDataFilter filter)
    {
        try
        {
            // Use the migrated data access layer
            return _exportDataAccess.GetDataReader(
                filter.FromMonth, filter.ToMonth, filter.HSCode, filter.Product, 
                filter.IEC, filter.Exporter, filter.ForeignCountry, filter.ForeignParty, filter.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing export query");
            throw;
        }
    }
    
    // Gets export data from the configured view
    public (SqlConnection connection, SqlDataReader reader, long recordCount) GetExportData(
        string fromMonth, string toMonth, string hsCode, string product, string iec, 
        string exporter, string country, string name, string port, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return _exportDataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, 
                exporter, country, name, port, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving export data");
            throw;
        }
    }

    // Gets import data using the migrated data access layer
    public Tuple<SqlConnection, SqlDataReader, long> GetImportData(
        string fromMonth, string toMonth, string hsCode, string product, string iec, 
        string importer, string country, string name, string port, 
        CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
    {
        try
        {
            return _importDataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, 
                importer, country, name, port, cancellationToken, viewName, storedProcedureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import data");
            throw;
        }
    }
    
    // Dispose resources
    public void Dispose()
    {
        // Dispose operational connection manager
        _operationalConnectionManager?.Dispose();
        
        // Unsubscribe from connection service events
        if (_connectionService != null)
        {
            _connectionService.PropertyChanged -= OnConnectionServicePropertyChanged;
        }
    }
}
