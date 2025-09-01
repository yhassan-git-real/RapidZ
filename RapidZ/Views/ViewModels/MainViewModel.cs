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

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ConfigurationService? _configService;
    private readonly ImportViewModel? _importViewModel;
    private readonly DatabaseService? _databaseService;
    private readonly ExportController? _exportController;
    private readonly ImportController? _importController;
    private readonly LogParserService? _logParserService;
    private readonly PathValidationService? _pathValidationService;
    
    private ServiceContainer? _services;
    
    private bool _isBusy;
    private double _progressPercentage = 0;
    private bool _canCancel = false;
    private ConnectionInfo _connectionInfo = new();
    private string _currentMode = "Export"; // Default to Export mode
    private SystemStatus _systemStatus = SystemStatus.Idle;
    
    // Execution summary properties
    private ExecutionSummary _lastExecution = ExecutionSummary.Empty;
    private bool _showExecutionSummary = false;
    private IDisposable? _logCheckSubscription;
    
    // Date properties
    private int _fromYear;
    private int _fromMonth;
    private int _toYear;
    private int _toMonth;
    
    // Database object selection properties
    private DbObjectOption? _selectedView;
    private DbObjectOption? _selectedStoredProcedure;
    private ObservableCollection<DbObjectOption>? _availableViews;
    private ObservableCollection<DbObjectOption>? _availableStoredProcedures;
    
    // Database object validation properties
    private bool _isViewValid = true;
    private bool _isStoredProcedureValid = true;
    private string _viewValidationMessage = string.Empty;
    private string _storedProcedureValidationMessage = string.Empty;

    // Export data filter for binding
    public ExportDataFilter ExportDataFilter { get; set; } = new();

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
    
    // Commands - using simple command pattern to avoid ReactiveCommand threading issues
    public ICommand ExportToExcelCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    public ICommand CancelImportCommand { get; private set; } = null!;
    public ICommand RefreshExecutionSummaryCommand { get; private set; } = null!;
    
   
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
    
    // Method to validate custom path settings
    private async void ValidateCustomPath()
    {
        // Check if path is provided but checkbox not selected - highlight checkbox
        if (!string.IsNullOrWhiteSpace(ExportDataFilter.CustomFilePath) && !ExportDataFilter.UseCustomPath)
        {
            ShouldHighlightCheckbox = true;
            IsCustomPathValid = false;
            CustomPathValidationMessage = "Please check 'Use Custom Path' to use the specified custom file path.";
            return;
        }
        else
        {
            ShouldHighlightCheckbox = false;
        }
        
        if (ExportDataFilter.UseCustomPath)
        {
            if (string.IsNullOrWhiteSpace(ExportDataFilter.CustomFilePath))
            {
                IsCustomPathValid = false;
                CustomPathValidationMessage = "Custom file path is required when 'Use Custom Path' is checked.";
                return;
            }
            
            // Use PathValidationService to validate the directory
            if (_pathValidationService != null)
            {
                var validationResult = await _pathValidationService.ValidateDirectoryPathAsync(ExportDataFilter.CustomFilePath);
                IsCustomPathValid = validationResult.IsValid;
                CustomPathValidationMessage = validationResult.ErrorMessage ?? string.Empty;
            }
        }
        else
        {
            IsCustomPathValid = true;
            CustomPathValidationMessage = string.Empty;
        }
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
    
    
    private void OnBusyStateChanged(bool isBusy)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = isBusy;
            CanCancel = isBusy;
            
            // If becoming idle (operation finished), set status to Idle
            // Specific success/failure states are set elsewhere based on results
            if (!isBusy)
            {
                // This will be overridden by success/failure handlers if needed
                SystemStatus = SystemStatus.Idle;
            }
        });
    }
    
    
    private async Task ExecuteExportToExcelCommandAsync()
    {
        try
        {
            // Validate custom path settings before proceeding
            if (!string.IsNullOrWhiteSpace(ExportDataFilter.CustomFilePath) && !ExportDataFilter.UseCustomPath)
            {
                // Custom path provided but checkbox not selected
                IsCustomPathValid = false;
                CustomPathValidationMessage = "Please check 'Use Custom Path' to use the specified custom file path.";
                SystemStatus = SystemStatus.Failed;
                
                // Reset to Idle after showing error
                await Task.Delay(3000);
                SystemStatus = SystemStatus.Idle;
                return;
            }
            
            // Validate that custom path is valid when checkbox is selected
            if (ExportDataFilter.UseCustomPath && !IsCustomPathValid)
            {
                SystemStatus = SystemStatus.Failed;
                
                // Reset to Idle after showing error
                await Task.Delay(3000);
                SystemStatus = SystemStatus.Idle;
                return;
            }
            
            // Set system status to Processing
            SystemStatus = SystemStatus.Processing;
            
            if (Services?.UIActionService != null)
            {
                // Prepare filter data before calling service
                PrepareFilterWithDefaults();
                SetCurrentFilterInService();
                
                // Call UIActionService
                await Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
                
                // If we get here, operation completed successfully
                SystemStatus = SystemStatus.Completed;
                
                // Reset to Idle after 3 seconds
                await Task.Delay(3000);
                SystemStatus = SystemStatus.Idle;
            }
        }
        catch (Exception)
        {
            // Set status to Failed on error
            SystemStatus = SystemStatus.Failed;
            
            // Reset to Idle after 5 seconds on error (longer to allow reading)
            await Task.Delay(5000);
            SystemStatus = SystemStatus.Idle;
        }
    }
    
    private async Task ExecuteClearFiltersCommandAsync()
    {
        try
        {
            // Brief "processing" status when clearing filters
            SystemStatus = SystemStatus.Processing;
            
            if (Services?.UIActionService != null)
            {
                Services.UIActionService.HandleReset();
            }
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Reset only input parameter fields
                ExportDataFilter.HSCode = string.Empty;
                ExportDataFilter.Product = string.Empty;
                ExportDataFilter.Exporter = string.Empty;
                ExportDataFilter.IEC = string.Empty;
                ExportDataFilter.ForeignParty = string.Empty;
                ExportDataFilter.ForeignCountry = string.Empty;
                ExportDataFilter.Port = string.Empty;
                
                // Preserve date fields, database selections, and mode
                // Do not reset FromYear, FromMonth, ToYear, ToMonth
                // Do not reset SelectedView or SelectedStoredProcedure
            });
            
            // Set status to completed briefly
            SystemStatus = SystemStatus.Completed;
            await Task.Delay(1500);
            SystemStatus = SystemStatus.Idle;
        }
        catch (Exception)
        {
            SystemStatus = SystemStatus.Failed;
            
            await Task.Delay(3000);
            SystemStatus = SystemStatus.Idle;
        }
    }
    
    private async Task ExecuteCancelImportCommandAsync()
    {
        try
        {
            SystemStatus = SystemStatus.Processing;
            
            if (Services?.UIActionService != null)
            {
                Services.UIActionService.HandleCancel();
            }
            
            // After cancellation, show brief "completed" status
            SystemStatus = SystemStatus.Completed;
            await Task.Delay(1500);
            SystemStatus = SystemStatus.Idle;
        }
        catch (Exception ex)
        {
            SystemStatus = SystemStatus.Failed;
            Console.WriteLine($"Error cancelling operation: {ex.Message}");
            
            await Task.Delay(3000);
            SystemStatus = SystemStatus.Idle;
        }
    }
    
    // Helper methods
    private void UpdateFromMonthValue()
    {
        ExportDataFilter.FromMonth = $"{_fromYear}{_fromMonth:D2}";
    }
    
    private void UpdateToMonthValue()
    {
        ExportDataFilter.ToMonth = $"{_toYear}{_toMonth:D2}";
    }
    
    private void SetCurrentFilterInService()
    {
        if (Services?.UIActionService is UIActionService uiActionService)
        {
            uiActionService.SetCurrentExportFilter(ExportDataFilter);
            uiActionService.SetSelectedView(SelectedView?.Name ?? "");
            uiActionService.SetSelectedStoredProcedure(SelectedStoredProcedure?.Name ?? "");
        }
    }
    
    private void PrepareFilterWithDefaults()
    {
        var defaultWildcard = "*"; // Use simple default
        
        ExportDataFilter.HSCode = string.IsNullOrEmpty(ExportDataFilter.HSCode) 
            ? defaultWildcard 
            : ExportDataFilter.HSCode;
            
        ExportDataFilter.Product = string.IsNullOrEmpty(ExportDataFilter.Product) 
            ? defaultWildcard 
            : ExportDataFilter.Product;
            
        ExportDataFilter.Exporter = string.IsNullOrEmpty(ExportDataFilter.Exporter) 
            ? defaultWildcard 
            : ExportDataFilter.Exporter;
            
        ExportDataFilter.IEC = string.IsNullOrEmpty(ExportDataFilter.IEC) 
            ? defaultWildcard 
            : ExportDataFilter.IEC;
            
        ExportDataFilter.ForeignParty = string.IsNullOrEmpty(ExportDataFilter.ForeignParty) 
            ? defaultWildcard 
            : ExportDataFilter.ForeignParty;
            
        ExportDataFilter.ForeignCountry = string.IsNullOrEmpty(ExportDataFilter.ForeignCountry) 
            ? defaultWildcard 
            : ExportDataFilter.ForeignCountry;
            
        ExportDataFilter.Port = string.IsNullOrEmpty(ExportDataFilter.Port) 
            ? defaultWildcard 
            : ExportDataFilter.Port;
    }
    
    /// <summary>
    /// Initialize default values
    /// </summary>
    private void InitializeDefaults()
    {
        var currentDate = DateTime.Now;
        _fromYear = currentDate.Year;
        _fromMonth = currentDate.Month;
        _toYear = currentDate.Year;
        _toMonth = currentDate.Month;
        
        ExportDataFilter = new ExportDataFilter
        {
            Mode = "Export", // Default mode is Export
            FromMonth = $"{_fromYear}{_fromMonth:D2}",
            ToMonth = $"{_toYear}{_toMonth:D2}",
            // Clear sample data while preserving placeholder functionality
            HSCode = string.Empty,
            Product = string.Empty,
            Exporter = string.Empty,
            IEC = string.Empty,
            ForeignParty = string.Empty,
            ForeignCountry = string.Empty,
            Port = string.Empty,
            CustomFilePath = string.Empty,
            UseCustomPath = false
        };
        
        // Set initial current mode
        _currentMode = "Export";
        
        // Add sample execution summary for better visual demonstration
        _lastExecution = new ExecutionSummary
        {
            Status = ExecutionStatus.Completed,
            FileName = "10_JAN25-SEP25EXP.xlsx",
            FilePath = "C:\\Export\\TradeData\\Reports\\10_JAN25-SEP25EXP.xlsx",
            RowCount = "30,840",
            TimeStamp = DateTime.Now.AddMinutes(-15),
            Duration = "00:13:056",
            Result = "Success - 10_JAN25-SEP25EXP.xlsx"
        };
        _showExecutionSummary = true;
        
        // Initialize database selections based on current mode when the view is loaded
        Dispatcher.UIThread.Post(() => UpdateDatabaseSelectionsForMode(), DispatcherPriority.Background);
        
        // Start the periodic log checking if log parser is available
        StartLogMonitoring();
    }
    
    /// <summary>
    /// Starts periodic checking of log files for execution summary updates
    /// </summary>
    private void StartLogMonitoring()
    {
        if (_logParserService == null)
            return;
            
        // Clean up any existing subscription
        _logCheckSubscription?.Dispose();
        
        // Set up periodic log checking
        _logCheckSubscription = Observable
            .Interval(TimeSpan.FromSeconds(5))
            // Make sure we observe on the main thread scheduler
            .ObserveOn(RxApp.MainThreadScheduler)
            // Use UI thread to handle the update
            .Subscribe(_ => 
            {
                Dispatcher.UIThread.Post(async () => 
                {
                    await UpdateExecutionSummaryAsync();
                });
            });
    }
    
    /// <summary>
    /// Updates the execution summary by checking the latest log files
    /// </summary>
    private async Task UpdateExecutionSummaryAsync()
    {
        if (_logParserService == null)
            return;
            
        try
        {
            // Get the latest execution summary from logs based on current mode
            var summary = await _logParserService.GetLatestExecutionSummaryAsync(_currentMode);
            
            // Ensure UI updates happen on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (summary.HasData)
                {
                    LastExecution = summary;
                    ShowExecutionSummary = true;
                }
            });
        }
        catch (Exception ex)
        {
            // Log the error but don't disturb the UI
            System.Diagnostics.Debug.WriteLine($"Error updating execution summary: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Forces an immediate update of the execution summary from logs
    /// </summary>
    public async Task RefreshExecutionSummaryAsync()
    {
        try
        {
            await UpdateExecutionSummaryAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Error refreshing execution summary: {ex.Message}");
            // Safe access to UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Ensure we still have a valid execution summary
                _lastExecution = ExecutionSummary.Empty;
            });
        }
    }
    
    /// <summary>
    /// Initialize commands using ReactiveCommand with proper UI thread scheduling
    /// </summary>
    private void InitializeCommands()
    {
        // Create observables for can execute validation
        var canExecuteGenerate = this.WhenAnyValue(x => x.IsBusy).Select(x => !x);
        var canExecuteExport = this.WhenAnyValue(x => x.IsBusy).Select(x => !x);
        var canExecuteClear = Observable.Return(true);
        var canExecuteCancel = this.WhenAnyValue(x => x.IsBusy);

        // Initialize ReactiveCommands with MainThreadScheduler to prevent threading issues            
        ExportToExcelCommand = ReactiveCommand.CreateFromTask(
            ExecuteExportToExcelCommandAsync, 
            canExecuteExport, 
            RxApp.MainThreadScheduler);
            
        ClearFiltersCommand = ReactiveCommand.CreateFromTask(
            ExecuteClearFiltersCommandAsync, 
            canExecuteClear, 
            RxApp.MainThreadScheduler);
            
        CancelImportCommand = ReactiveCommand.CreateFromTask(
            ExecuteCancelImportCommandAsync, 
            canExecuteCancel, 
            RxApp.MainThreadScheduler);
            
        // Command to refresh execution summary data from logs
        RefreshExecutionSummaryCommand = ReactiveCommand.CreateFromTask(
            async () => 
            {
                // Use UI thread to safely update UI
                await Dispatcher.UIThread.InvokeAsync(async () => 
                {
                    await RefreshExecutionSummaryAsync();
                });
            },
            Observable.Return(true),
            RxApp.MainThreadScheduler);
    }
    
    /// <summary>
    /// Initialize collections for dropdown binding
    /// </summary>
    private void InitializeCollections()
    {
        _availableViews = new ObservableCollection<DbObjectOption>();
        _availableStoredProcedures = new ObservableCollection<DbObjectOption>();
    }
    
    /// <summary>
    /// Load database objects from services when available
    /// </summary>
    private void LoadDataDirectlyFromConfiguration()
    {
        // Reset validation states
        IsViewValid = true;
        IsStoredProcedureValid = true;
        ViewValidationMessage = string.Empty;
        StoredProcedureValidationMessage = string.Empty;
        if (Services == null)
            return;

        try
        {
            // Clear existing collections
            AvailableViews?.Clear();
            AvailableStoredProcedures?.Clear();

            string currentMode = ExportDataFilter?.Mode ?? "Export";

            if (currentMode == "Export" && Services.ExportObjectValidationService != null)
            {
                var exportViews = Services.ExportObjectValidationService.GetAvailableViews();
                var exportProcs = Services.ExportObjectValidationService.GetAvailableStoredProcedures();
                
                AvailableViews = new ObservableCollection<DbObjectOption>(exportViews);
                AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(exportProcs);

                var defaultView = Services.ExportObjectValidationService.GetDefaultViewName();
                var defaultProc = Services.ExportObjectValidationService.GetDefaultStoredProcedureName();
                
                SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
            }
            else if (currentMode == "Import" && Services.ImportObjectValidationService != null)
            {
                var importViews = Services.ImportObjectValidationService.GetAvailableViews();
                var importProcs = Services.ImportObjectValidationService.GetAvailableStoredProcedures();
                
                AvailableViews = new ObservableCollection<DbObjectOption>(importViews);
                AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(importProcs);

                var defaultView = Services.ImportObjectValidationService.GetDefaultViewName();
                var defaultProc = Services.ImportObjectValidationService.GetDefaultStoredProcedureName();
                
                SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
            }
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to load database objects: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Updates database selections (views, stored procedures) based on the current application mode
    /// </summary>
    private void UpdateDatabaseSelectionsForMode()
    {
        if (Services == null)
        {
            
            Console.WriteLine("Services not initialized");
            return;
        }

        try
        {
            // Clear existing collections
            AvailableViews?.Clear();
            AvailableStoredProcedures?.Clear();

            // Use the current mode to determine which service to use
            string mode = CurrentMode ?? "Export"; // Default to Export if not set
            
            if (mode == "Export")
            {
                // Set database objects for Export mode
                if (Services.ExportObjectValidationService != null)
                {
                    var exportViews = Services.ExportObjectValidationService.GetAvailableViews();
                    var exportProcs = Services.ExportObjectValidationService.GetAvailableStoredProcedures();
                    
                    AvailableViews = new ObservableCollection<DbObjectOption>(exportViews);
                    AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(exportProcs);
    
                    var defaultView = Services.ExportObjectValidationService.GetDefaultViewName();
                    var defaultProc = Services.ExportObjectValidationService.GetDefaultStoredProcedureName();
                    
                    SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                    SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
                    
                    // Export mode: Database objects loaded successfully
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects();
                }
                else
                {
                    // Export validation service not available
                    Console.WriteLine("Export validation service not available");
                }
            }
            else if (mode == "Import")
            {
                // Set database objects for Import mode
                if (Services.ImportObjectValidationService != null)
                {
                    var importViews = Services.ImportObjectValidationService.GetAvailableViews();
                    var importProcs = Services.ImportObjectValidationService.GetAvailableStoredProcedures();
                    
                    AvailableViews = new ObservableCollection<DbObjectOption>(importViews);
                    AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(importProcs);
    
                    var defaultView = Services.ImportObjectValidationService.GetDefaultViewName();
                    var defaultProc = Services.ImportObjectValidationService.GetDefaultStoredProcedureName();
                    
                    SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                    SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
                    
                    // Import mode: Database objects loaded successfully
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects();
                }
                else
                {
                    // Import validation service not available
                    Console.WriteLine("Import validation service not available");
                }
            }
            
            // Update ExporterLabelText
            this.RaisePropertyChanged(nameof(ExporterLabelText));
            this.RaisePropertyChanged(nameof(FileLocationLabelText));
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to update database selections: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates the selected view and stored procedure against the database
    /// </summary>
    private void ValidateSelectedDatabaseObjects()
    {
        // If services or DatabaseObjectValidator are not available, can't validate
        if (Services?.DatabaseObjectValidator == null || SelectedView == null || SelectedStoredProcedure == null)
        {
            return;
        }

        string viewName = SelectedView.Name;
        string procName = SelectedStoredProcedure.Name;

        // Validate the view
        IsViewValid = Services.DatabaseObjectValidator.ViewExists(viewName);
        ViewValidationMessage = IsViewValid ? string.Empty : 
            "Object does not exist in database";

        // Validate the stored procedure
        IsStoredProcedureValid = Services.DatabaseObjectValidator.StoredProcedureExists(procName);
        StoredProcedureValidationMessage = IsStoredProcedureValid ? string.Empty : 
            "Object does not exist in database";
    }

    /// <summary>
    /// Implements IDisposable to clean up resources
    /// </summary>
    public void Dispose()
    {
        _logCheckSubscription?.Dispose();
    }
}
