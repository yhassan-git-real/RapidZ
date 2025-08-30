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

public class MainViewModel : ViewModelBase
{
    private readonly ConfigurationService? _configService;
    private readonly ImportViewModel? _importViewModel;
    private readonly DatabaseService? _databaseService;
    private readonly ExportController? _exportController;
    private readonly ImportController? _importController;
    
    private ServiceContainer? _services;
    
    private bool _isBusy;
    private double _progressPercentage = 0;
    private bool _canCancel = false;
    private ConnectionInfo _connectionInfo = new();
    private string _currentMode = "Export"; // Default to Export mode
    
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

    // Export data filter for binding
    public ExportDataFilter ExportDataFilter { get; set; } = new();

    // Parameterless constructor for UI testing
    public MainViewModel()
    {
        InitializeDefaults();
        InitializeCommands();
        InitializeCollections();
    }

    public MainViewModel(
        ConfigurationService configService, 
        ImportViewModel importViewModel,
        DatabaseService databaseService,
        ExportController exportController,
        ImportController importController)
    {
        _configService = configService;
        _importViewModel = importViewModel;
        _databaseService = databaseService;
        _exportController = exportController;
        _importController = importController;
        
        InitializeDefaults();
        InitializeCommands();
        InitializeCollections();
        
        // Initialize connection info
        _connectionInfo = _databaseService.GetConnectionInfo();
        
        // Subscribe to DatabaseService property changes for UI binding
        _databaseService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DatabaseService.IsConnected) || 
                e.PropertyName == nameof(DatabaseService.ConnectionStatus))
            {
                // Update the connection info when these properties change
                ConnectionInfo = _databaseService.GetConnectionInfo();
                
                // Also raise property changed for status message
                this.RaisePropertyChanged(nameof(StatusMessage));
            }
        };
        
        // Set up timer to refresh connection info every minute when not busy
        // (Less frequent than DB checks, but still responsive enough for the UI)
        var timer = new System.Timers.Timer(60000); // 1 minute
        timer.Elapsed += (s, e) => 
        {
            Dispatcher.UIThread.InvokeAsync(() => 
            {
                if (_databaseService != null && !IsBusy)
                {
                    // Update connection info and ensure status message is refreshed too
                    ConnectionInfo = _databaseService.GetConnectionInfo();
                    this.RaisePropertyChanged(nameof(StatusMessage));
                }
            });
        };
        timer.AutoReset = true;
        timer.Start();
    }
    
    // Commands - using simple command pattern to avoid ReactiveCommand threading issues
    public ICommand RunQueryCommand { get; private set; } = null!;
    public ICommand ExportToExcelCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    public ICommand CancelImportCommand { get; private set; } = null!;
    
   
    public bool IsBusy
    {
        get => _isBusy;
        set 
        { 
            this.RaiseAndSetIfChanged(ref _isBusy, value);
            
            // Pause or resume connection checks based on busy state
            if (value) 
            {
                // Pause connection checks when application is busy with operations
                DatabaseConnectionService.Instance.PauseConnectionChecks();
            }
            else 
            {
                // Resume connection checks when operations are complete
                DatabaseConnectionService.Instance.ResumeConnectionChecks();
            }
        }
    }
    
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
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
        get => _connectionInfo.StatusMessage;
        set 
        {
            _connectionInfo.StatusMessage = value;
            this.RaisePropertyChanged(nameof(StatusMessage));
        }
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
                
                // Raise property change for all filter properties to refresh UI
                this.RaisePropertyChanged(nameof(ExportDataFilter));
            }
        }
    }
    
    // Dynamic label for Exporter/Importer field
    public string ExporterLabelText => CurrentMode == "Import" ? "Importer" : "Exporter";
    
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
        set => this.RaiseAndSetIfChanged(ref _selectedView, value);
    }
    
    public DbObjectOption? SelectedStoredProcedure
    {
        get => _selectedStoredProcedure;
        set => this.RaiseAndSetIfChanged(ref _selectedStoredProcedure, value);
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
        });
    }
    
    
    private async Task ExecuteExportToExcelCommandAsync()
    {
        try
        {
            if (Services?.UIActionService != null)
            {
                // Prepare filter data before calling service
                PrepareFilterWithDefaults();
                SetCurrentFilterInService();
                
                // Call UIActionService
                await Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Error in export operation: {ex.Message}");
        }
    }
    
    private Task ExecuteRunQueryCommandAsync()
    {
        // Query functionality not implemented 
        Console.WriteLine("Query functionality not implemented yet");
        return Task.CompletedTask;
    }
    
    private async Task ExecuteClearFiltersCommandAsync()
    {
        if (Services?.UIActionService != null)
        {
            Services.UIActionService.HandleReset();
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Reset form fields
            ExportDataFilter.HSCode = string.Empty;
            ExportDataFilter.Product = string.Empty;
            ExportDataFilter.Exporter = string.Empty;
            ExportDataFilter.IEC = string.Empty;
            ExportDataFilter.ForeignParty = string.Empty;
            ExportDataFilter.ForeignCountry = string.Empty;
            ExportDataFilter.Port = string.Empty;
            
            var currentDate = DateTime.Now;
            FromYear = currentDate.Year;
            FromMonth = currentDate.Month;
            ToYear = currentDate.Year;
            ToMonth = currentDate.Month;
            
            SelectedView = null;
            SelectedStoredProcedure = null;
        });
    }
    
    private Task ExecuteCancelImportCommandAsync()
    {
        if (Services?.UIActionService != null)
        {
            Services.UIActionService.HandleCancel();
        }
        return Task.CompletedTask;
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
            ToMonth = $"{_toYear}{_toMonth:D2}"
        };
        
        // Set initial current mode
        _currentMode = "Export";
        
        
        
        // Initialize database selections based on current mode when the view is loaded
        Dispatcher.UIThread.Post(() => UpdateDatabaseSelectionsForMode(), DispatcherPriority.Background);
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
        RunQueryCommand = ReactiveCommand.CreateFromTask(
            ExecuteRunQueryCommandAsync, 
            canExecuteGenerate, 
            RxApp.MainThreadScheduler);
            
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
                }
                else
                {
                    // Import validation service not available
                    Console.WriteLine("Import validation service not available");
                }
            }
            
            // Update ExporterLabelText
            this.RaisePropertyChanged(nameof(ExporterLabelText));
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to update database selections: {ex.Message}");
        }
    }
}
