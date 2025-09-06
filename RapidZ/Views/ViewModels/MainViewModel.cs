using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using RapidZ.Views.Models;
using RapidZ.Core.Models;
using RapidZ.Core.Controllers;
using RapidZ.Core.Services;
using RapidZ.Core.Helpers;
using RapidZ.Features.Common.ViewModels;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Import.Services;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace RapidZ.Views.ViewModels;

public partial class MainViewModel : ReactiveObject, IDisposable
{
    private readonly ConfigurationService? _configService;
    private readonly ImportViewModel? _importViewModel;
    private readonly DatabaseService? _databaseService;
    private readonly ExportController? _exportController;
    private readonly ImportController? _importController;
    private readonly LogParserService? _logParserService;
    private readonly PathValidationService? _pathValidationService;
    
    private ServiceContainer? _services;

    // Parameterless constructor for UI testing
    public MainViewModel()
    {
        InitializeDefaults();
        InitializeCommands();
        InitializeCollections();
        // No log parser in the default constructor
    }

    public MainViewModel(
        ConfigurationService configService, 
        ImportViewModel importViewModel,
        DatabaseService databaseService,
        ExportController exportController,
        ImportController importController,
        PathValidationService pathValidationService)
    {
        _configService = configService;
        _importViewModel = importViewModel;
        _databaseService = databaseService;
        _exportController = exportController;
        _importController = importController;
        _pathValidationService = pathValidationService;
        _logParserService = new LogParserService(AppDomain.CurrentDomain.BaseDirectory);
        
        // Initialize defaults first
        InitializeDefaults();
        // Initialize commands after defaults
        InitializeCommands();
        // Initialize collections 
        InitializeCollections();
        
        // Ensure we have a valid execution summary to avoid null reference exceptions
        _lastExecution = ExecutionSummary.Empty;
        
        // Subscribe to ExportDataFilter property changes for validation
        ExportDataFilter.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ExportDataFilter.UseCustomPath) || 
                e.PropertyName == nameof(ExportDataFilter.CustomFilePath))
            {
                ValidateCustomPath();
            }
            
            // Validate input parameters when any parameter field changes
            if (e.PropertyName == nameof(ExportDataFilter.HSCode) ||
                e.PropertyName == nameof(ExportDataFilter.Product) ||
                e.PropertyName == nameof(ExportDataFilter.Exporter) ||
                e.PropertyName == nameof(ExportDataFilter.IEC) ||
                e.PropertyName == nameof(ExportDataFilter.ForeignParty) ||
                e.PropertyName == nameof(ExportDataFilter.ForeignCountry) ||
                e.PropertyName == nameof(ExportDataFilter.Port))
            {
                ValidateInputParameters();
            }
        };
        
        // Initialize connection info
        _connectionInfo = _databaseService.GetConnectionInfo();
        
        // Subscribe to DatabaseService property changes for UI binding
        _databaseService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DatabaseService.IsConnected) || 
                e.PropertyName == nameof(DatabaseService.ConnectionStatus))
            {
                // Update the connection info when these properties change
                var newConnectionInfo = _databaseService.GetConnectionInfo();
                
                // Only update if connection info actually changed to reduce UI overhead
                if (!ConnectionInfoEquals(_connectionInfo, newConnectionInfo))
                {
                    ConnectionInfo = newConnectionInfo;
                    this.RaisePropertyChanged(nameof(StatusMessage));
                }
            }
        };
        
        // Set up timer to refresh connection info less frequently to reduce UI overhead
        // Only refresh when not busy and reduce frequency to 2 minutes
        var timer = new System.Timers.Timer(120000); // 2 minutes
        timer.Elapsed += (s, e) => 
        {
            // Only update if not busy to avoid unnecessary UI updates during operations
            if (_databaseService != null && !IsBusy)
            {
                Dispatcher.UIThread.InvokeAsync(() => 
                {
                    // Update connection info efficiently
                    var newConnectionInfo = _databaseService.GetConnectionInfo();
                    
                    // Only update if connection info actually changed to reduce UI redraws
                    if (!ConnectionInfoEquals(_connectionInfo, newConnectionInfo))
                    {
                        ConnectionInfo = newConnectionInfo;
                        this.RaisePropertyChanged(nameof(StatusMessage));
                    }
                });
            }
        };
        timer.AutoReset = true;
        timer.Start();
    }
    
   
    public bool IsBusy
    {
        get => _isBusy;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _isBusy, value);
            
            // Update system status based on busy state
            if (value)
            {
                // When becoming busy, change status to processing
                SystemStatus = SystemStatus.Processing;
                
                // Pause connection checks when application is busy with operations
                DatabaseConnectionService.Instance.PauseConnectionChecks();
            }
            else 
            {
                // When no longer busy, don't immediately change the status
                // This will be set by the operation result (Completed or Failed)
                
                // Resume connection checks when operations are complete
                DatabaseConnectionService.Instance.ResumeConnectionChecks();
            }
        }
    }
    
    public SystemStatus SystemStatus
    {
        get => _systemStatus;
        set
        {
            this.RaiseAndSetIfChanged(ref _systemStatus, value);
            this.RaisePropertyChanged(nameof(SystemStatusMessage));
            this.RaisePropertyChanged(nameof(StatusIndicatorColor));
            this.RaisePropertyChanged(nameof(IsProcessing));
            this.RaisePropertyChanged(nameof(IsIndeterminateProgress));
            
            // When status changes to Completed or Failed, update execution summary after a short delay
            if (value == SystemStatus.Completed || value == SystemStatus.Failed)
            {
                // Schedule execution summary refresh after a short delay to allow log files to be written
                _ = Task.Delay(1500).ContinueWith(async _ => 
                {
                    // Make sure we dispatch back to UI thread for UI updates
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await UpdateExecutionSummaryAsync();
                    });
                });
            }
        }
    }
    
    public string SystemStatusMessage => _systemStatus.GetStatusMessage();
    
    public IBrush StatusIndicatorColor => _systemStatus.GetStatusColor();
    
    public bool IsProcessing => _systemStatus == SystemStatus.Processing;
    
    public bool IsIndeterminateProgress => _systemStatus == SystemStatus.Processing && _progressPercentage == 0;
    
    public string ProgressText => $"{(int)_progressPercentage}%";
    
    // Execution Summary Properties
    public ExecutionSummary LastExecution 
    {
        get => _lastExecution ?? ExecutionSummary.Empty;
        set
        {
            if (value == null)
                return;
                
            this.RaiseAndSetIfChanged(ref _lastExecution, value);
            this.RaisePropertyChanged(nameof(ExecutionStatusColor));
        }
    }
    
    public bool ShowExecutionSummary
    {
        get => _showExecutionSummary;
        set => this.RaiseAndSetIfChanged(ref _showExecutionSummary, value);
    }
    
    // Helper property to determine status color based on result
    public IBrush ExecutionStatusColor
    {
        get 
        {
            // Null check to avoid NullReferenceException
            if (_lastExecution == null) 
                return new SolidColorBrush(Color.FromRgb(153, 153, 153)); // Grey
                
            return _lastExecution.Status switch
            {
                ExecutionStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
                ExecutionStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)),      // Red
                _ => new SolidColorBrush(Color.FromRgb(153, 153, 153))                          // Grey
            };
        }
    }
    
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set 
        {
            this.RaiseAndSetIfChanged(ref _progressPercentage, value);
            this.RaisePropertyChanged(nameof(IsIndeterminateProgress));
        }
    }
    
    public bool CanCancel
    {
        get => _canCancel;
        set => this.RaiseAndSetIfChanged(ref _canCancel, value);
    }
    
    public ConnectionInfo ConnectionInfo
    {
        get => _connectionInfo;
        set => this.RaiseAndSetIfChanged(ref _connectionInfo, value);
    }
    
    public string StatusMessage 
    {
        get => _connectionInfo?.StatusMessage ?? "Ready";
        set 
        {
            if (_connectionInfo != null && _connectionInfo.StatusMessage != value)
            {
                _connectionInfo.StatusMessage = value;
                this.RaisePropertyChanged(nameof(StatusMessage));
            }
        }
    }
    
    // Helper method to compare connection info efficiently
    private bool ConnectionInfoEquals(ConnectionInfo? info1, ConnectionInfo? info2)
    {
        if (info1 == null && info2 == null) return true;
        if (info1 == null || info2 == null) return false;
        
        return info1.ServerName == info2.ServerName &&
               info1.DatabaseName == info2.DatabaseName &&
               info1.UserName == info2.UserName &&
               info1.IsConnected == info2.IsConnected &&
               info1.ResponseTime == info2.ResponseTime &&
               info1.StatusMessage == info2.StatusMessage;
    }
    
    // Current mode (Export or Import) property
    public string CurrentMode
    {
        get => _currentMode;
        set
        {
            string oldValue = _currentMode;
            this.RaiseAndSetIfChanged(ref _currentMode, value);
            
            // Check if the value actually changed
            if (oldValue != _currentMode)
            {
                // When mode changes, update database selections and labels
                UpdateDatabaseSelectionsForMode();
                
                // Notify UI that export filter mode has changed
                ExportDataFilter.Mode = value;
                this.RaisePropertyChanged(nameof(ExporterLabelText));
                this.RaisePropertyChanged(nameof(FileLocationLabelText));
                
                // Raise property change for all filter properties to refresh UI
                this.RaisePropertyChanged(nameof(ExportDataFilter));
            }
        }
    }
    
    // Dynamic label for Exporter/Importer field
    public string ExporterLabelText => CurrentMode == "Import" ? "Importer" : "Exporter";
    
    // Dynamic label for File Location field
    public string FileLocationLabelText => CurrentMode == "Import" ? "Import File Location" : "Export File Location";
    
    // Validation state for custom file path
    private bool _isCustomPathValid = true;
    public bool IsCustomPathValid
    {
        get => _isCustomPathValid;
        set => this.RaiseAndSetIfChanged(ref _isCustomPathValid, value);
    }
    
    // Validation message for custom file path
    private string _customPathValidationMessage = string.Empty;
    public string CustomPathValidationMessage
    {
        get => _customPathValidationMessage;
        set => this.RaiseAndSetIfChanged(ref _customPathValidationMessage, value);
    }
    
    // Property to highlight checkbox when path is provided but checkbox not selected
    private bool _shouldHighlightCheckbox = false;
    public bool ShouldHighlightCheckbox
    {
        get => _shouldHighlightCheckbox;
        set => this.RaiseAndSetIfChanged(ref _shouldHighlightCheckbox, value);
    }
    
    // Date properties for UI binding
    public int FromYear
    {
        get => _fromYear;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _fromYear, value);
            UpdateFromMonthValue();
        }
    }
    
    public int FromMonth
    {
        get => _fromMonth;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _fromMonth, value);
            UpdateFromMonthValue();
        }
    }
    
    public int ToYear
    {
        get => _toYear;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _toYear, value);
            UpdateToMonthValue();
        }
    }
    
    public int ToMonth
    {
        get => _toMonth;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _toMonth, value);
            UpdateToMonthValue();
        }
    }
    
    // Database object selection properties
    public DbObjectOption? SelectedView
    {
        get => _selectedView;
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedView, value);
            // Validate the database object immediately after selection
            ValidateSelectedDatabaseObjects();
        }
    }
    
    public DbObjectOption? SelectedStoredProcedure
    {
        get => _selectedStoredProcedure;
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedStoredProcedure, value);
            // Validate the database object immediately after selection
            ValidateSelectedDatabaseObjects();
        }
    }
    
    // Database object validation properties
    public bool IsViewValid
    {
        get => _isViewValid;
        private set => this.RaiseAndSetIfChanged(ref _isViewValid, value);
    }
    
    public bool IsStoredProcedureValid
    {
        get => _isStoredProcedureValid;
        private set => this.RaiseAndSetIfChanged(ref _isStoredProcedureValid, value);
    }
    
    public string ViewValidationMessage
    {
        get => _viewValidationMessage;
        private set => this.RaiseAndSetIfChanged(ref _viewValidationMessage, value);
    }
    
    public string StoredProcedureValidationMessage
    {
        get => _storedProcedureValidationMessage;
        private set => this.RaiseAndSetIfChanged(ref _storedProcedureValidationMessage, value);
    }
    
    public bool AreMandatoryFieldsValid
    {
        get => _areMandatoryFieldsValid;
        private set => this.RaiseAndSetIfChanged(ref _areMandatoryFieldsValid, value);
    }
    
    public string MandatoryFieldsValidationMessage
    {
        get => _mandatoryFieldsValidationMessage;
        set => this.RaiseAndSetIfChanged(ref _mandatoryFieldsValidationMessage, value);
    }
    
    /// <summary>
    /// Gets or sets whether input parameters are valid (at least one parameter has a value excluding '%')
    /// </summary>
    public bool AreInputParametersValid
    {
        get => _areInputParametersValid;
        set => this.RaiseAndSetIfChanged(ref _areInputParametersValid, value);
    }
    
    /// <summary>
    /// Gets or sets the validation message for input parameters
    /// </summary>
    public string InputParametersValidationMessage
    {
        get => _inputParametersValidationMessage;
        set => this.RaiseAndSetIfChanged(ref _inputParametersValidationMessage, value);
    }
    
    public ObservableCollection<DbObjectOption>? AvailableViews
    {
        get => _availableViews;
        set => this.RaiseAndSetIfChanged(ref _availableViews, value);
    }
    
    public ObservableCollection<DbObjectOption>? AvailableStoredProcedures
    {
        get => _availableStoredProcedures;
        set => this.RaiseAndSetIfChanged(ref _availableStoredProcedures, value);
    }
    
    // Services container - following TradeDataHub pattern
    public ServiceContainer? Services
    {
        get => _services;
        set
        {
            _services = value;
            this.RaisePropertyChanged();
            
            if (_services != null)
            {
                LoadDataDirectlyFromConfiguration();
                
                
                if (_services.UIActionService != null)
                {
                    _services.UIActionService.BusyStateChanged += OnBusyStateChanged;
                }
            }
        }
    }
    
    
    /// <summary>
    /// Implements IDisposable to clean up resources
    /// </summary>
    public void Dispose()
    {
        _logCheckSubscription?.Dispose();
    }
}
