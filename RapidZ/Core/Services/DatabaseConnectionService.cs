using System;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using RapidZ.Core.Database;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace RapidZ.Core.Services
{
    public class DatabaseConnectionInfo
    {
        public string ServerName { get; set; } = "Unknown";
        public string DatabaseName { get; set; } = "Unknown";
        public string UserAccount { get; set; } = "Unknown";
        public string ConnectionStatus { get; set; } = "Disconnected";
        public string StatusColor { get; set; } = "#dc3545"; // Red by default
        public DateTime LastChecked { get; set; } = DateTime.Now;
        public int ResponseTime { get; set; } = 0; // Response time in milliseconds
    }

    public class DatabaseConnectionService : INotifyPropertyChanged
    {
        private static DatabaseConnectionService? _instance;
        private Timer? _connectionCheckTimer;
        private DatabaseConnectionInfo _connectionInfo;
        private readonly SharedDatabaseSettings _dbSettings;
        private bool _isPaused = false; // Flag to pause connection checks
        private int _lastResponseTime = 0;
        private bool _isStartupTestMode = false; // Flag for startup testing

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static DatabaseConnectionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseConnectionService();
                }
                return _instance;
            }
        }

        private DatabaseConnectionService()
        {
            _connectionInfo = new DatabaseConnectionInfo();
            _dbSettings = LoadDatabaseSettings();
            
            // Parse connection string initially
            ParseConnectionString();
            
            // Don't start timer automatically - will be controlled by startup test
        }

        public DatabaseConnectionInfo ConnectionInfo
        {
            get => _connectionInfo;
            private set
            {
                _connectionInfo = value;
                OnPropertyChanged();
            }
        }

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

        private void ParseConnectionString()
        {
            try
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(_dbSettings.ConnectionString);
                
                var newInfo = new DatabaseConnectionInfo
                {
                    ServerName = connectionStringBuilder.DataSource ?? "Unknown",
                    DatabaseName = connectionStringBuilder.InitialCatalog ?? "Unknown",
                    UserAccount = !string.IsNullOrEmpty(connectionStringBuilder.UserID) 
                        ? connectionStringBuilder.UserID 
                        : connectionStringBuilder.IntegratedSecurity ? "Windows Auth" : "Unknown",
                    ConnectionStatus = "Disconnected",
                    StatusColor = "#dc3545", // Red for disconnected initially
                    LastChecked = DateTime.Now
                };

                ConnectionInfo = newInfo;
            }
            catch (Exception)
            {
                ConnectionInfo = new DatabaseConnectionInfo
                {
                    ServerName = "Configuration Error",
                    DatabaseName = "Configuration Error",
                    UserAccount = "Configuration Error",
                    ConnectionStatus = "Config Error",
                    StatusColor = "#dc3545",
                    LastChecked = DateTime.Now
                };
            }
        }

        private async void CheckConnectionStatus(object? state)
        {
            // Skip connection check if paused (during operations)
            if (!_isPaused)
            {
                await CheckConnectionStatusAsync();
            }
        }

        public async Task CheckConnectionStatusAsync()
        {
            try
            {
                // Set status to checking initially to ensure proper UI update
                var checkingInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Checking...",
                    StatusColor = "#ffc107", // Yellow for checking
                    LastChecked = DateTime.Now,
                    ResponseTime = _lastResponseTime
                };
                
                ConnectionInfo = checkingInfo;
                
                // Actually check the connection
                var startTime = DateTime.Now;
                using var connection = new SqlConnection(_dbSettings.ConnectionString);
                await connection.OpenAsync();
                _lastResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                
                // Update status to connected
                var updatedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Connected",
                    StatusColor = "#28a745", // Green for connected
                    LastChecked = DateTime.Now,
                    ResponseTime = _lastResponseTime
                };

                ConnectionInfo = updatedInfo;
                
                // Explicitly trigger property changed
                OnPropertyChanged(nameof(ConnectionInfo));
            }
            catch (Exception)
            {
                // Update status to disconnected
                var updatedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Disconnected",
                    StatusColor = "#dc3545", // Red for disconnected
                    LastChecked = DateTime.Now
                };

                ConnectionInfo = updatedInfo;
                
                // Explicitly trigger property changed
                OnPropertyChanged(nameof(ConnectionInfo));
            }
        }



        // Startup testing method - test connection once and disconnect (optimized)
        public async Task<bool> TestConnectionOnStartupAsync()
        {
            _isStartupTestMode = true;
            
            try
            {
                // Test the connection with optimized timeout
                var startTime = DateTime.Now;
                
                // Create connection string with reduced timeout for faster startup
                var connectionStringBuilder = new SqlConnectionStringBuilder(_dbSettings.ConnectionString)
                {
                    ConnectTimeout = 3 // Reduced timeout for faster startup
                };
                
                using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();
                _lastResponseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                
                // Connection successful - set to disconnected (as per requirement)
                var disconnectedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Ready",
                    StatusColor = "#28a745", // Green for ready
                    LastChecked = DateTime.Now,
                    ResponseTime = _lastResponseTime
                };

                ConnectionInfo = disconnectedInfo;
                OnPropertyChanged(nameof(ConnectionInfo));
                
                _isStartupTestMode = false;
                return true;
            }
            catch (Exception)
            {
                // Connection failed - set minimal info to avoid UI delays
                var failedInfo = new DatabaseConnectionInfo
                {
                    ServerName = ConnectionInfo.ServerName,
                    DatabaseName = ConnectionInfo.DatabaseName,
                    UserAccount = ConnectionInfo.UserAccount,
                    ConnectionStatus = "Connection Failed",
                    StatusColor = "#dc3545", // Red for failed
                    LastChecked = DateTime.Now,
                    ResponseTime = 0
                };

                ConnectionInfo = failedInfo;
                OnPropertyChanged(nameof(ConnectionInfo));
                
                _isStartupTestMode = false;
                return false;
            }
        }

        // Operational connection methods for active database operations
        public void SetOperationalConnected()
        {
            var connectedInfo = new DatabaseConnectionInfo
            {
                ServerName = ConnectionInfo.ServerName,
                DatabaseName = ConnectionInfo.DatabaseName,
                UserAccount = ConnectionInfo.UserAccount,
                ConnectionStatus = "Connected",
                StatusColor = "#28a745", // Green for connected
                LastChecked = DateTime.Now,
                ResponseTime = _lastResponseTime
            };

            ConnectionInfo = connectedInfo;
            OnPropertyChanged(nameof(ConnectionInfo));
        }

        public void SetOperationalDisconnected()
        {
            var disconnectedInfo = new DatabaseConnectionInfo
            {
                ServerName = ConnectionInfo.ServerName,
                DatabaseName = ConnectionInfo.DatabaseName,
                UserAccount = ConnectionInfo.UserAccount,
                ConnectionStatus = "Disconnected",
                StatusColor = "#dc3545", // Red for disconnected
                LastChecked = DateTime.Now,
                ResponseTime = _lastResponseTime
            };

            ConnectionInfo = disconnectedInfo;
            OnPropertyChanged(nameof(ConnectionInfo));
        }

        // Start continuous monitoring with optimized frequency
        public void StartContinuousMonitoring()
        {
            if (_connectionCheckTimer == null)
            {
                // Increase interval to 10 minutes to reduce overhead
                // Start after 30 seconds instead of immediately to avoid startup conflicts
                _connectionCheckTimer = new Timer(CheckConnectionStatus, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(10));
            }
        }

        // Stop continuous monitoring
        public void StopContinuousMonitoring()
        {
            _connectionCheckTimer?.Dispose();
            _connectionCheckTimer = null;
        }

        // Methods to pause and resume connection checks during operations
        public void PauseConnectionChecks()
        {
            _isPaused = true;
        }
        
        public void ResumeConnectionChecks()
        {
            _isPaused = false;
            
            // Only check if timer is running and avoid immediate check to prevent UI blocking
            if (_connectionCheckTimer != null)
            {
                // Delay the check by 2 seconds to allow UI to settle after operations
                Task.Delay(2000).ContinueWith(async _ => 
                {
                    if (!_isPaused) // Double-check pause state
                    {
                        await CheckConnectionStatusAsync();
                    }
                });
            }
        }
        
        public void Dispose()
        {
            _connectionCheckTimer?.Dispose();
        }
    }
}