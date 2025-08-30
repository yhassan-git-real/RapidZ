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
    // Status panel properties
    private string _statusMessage = string.Empty;
    private IBrush _statusMessageColor = Brushes.White;
    private string _statusTitle = string.Empty;
    private string _statusDetails = string.Empty;
    private StatusType _statusType = StatusType.None;
    private bool _statusHasAction = false;
    private string _statusActionText = string.Empty;
    private ICommand? _statusActionCommand = null;
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
                this.RaisePropertyChanged(nameof(ConnectionInfo));
            }
        };
    }
    
    // Commands - using simple command pattern to avoid ReactiveCommand threading issues
    public ICommand RunQueryCommand { get; private set; } = null!;
    public ICommand ExportToExcelCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    public ICommand CancelImportCommand { get; private set; } = null!;
    
    // Status Panel Properties
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
    
    public IBrush StatusMessageColor
    {
        get => _statusMessageColor;
        set => this.RaiseAndSetIfChanged(ref _statusMessageColor, value);
    }
    
    public string StatusTitle
    {
        get => _statusTitle;
        set => this.RaiseAndSetIfChanged(ref _statusTitle, value);
    }
    
    public string StatusDetails
    {
        get => _statusDetails;
        set => this.RaiseAndSetIfChanged(ref _statusDetails, value);
    }
    
    public StatusType StatusType
    {
        get => _statusType;
        set => this.RaiseAndSetIfChanged(ref _statusType, value);
    }
    
    public bool StatusHasAction
    {
        get => _statusHasAction;
        set => this.RaiseAndSetIfChanged(ref _statusHasAction, value);
    }
    
    public string StatusActionText
    {
        get => _statusActionText;
        set => this.RaiseAndSetIfChanged(ref _statusActionText, value);
    }
    
    public ICommand? StatusActionCommand
    {
        get => _statusActionCommand;
        set => this.RaiseAndSetIfChanged(ref _statusActionCommand, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
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
                
                // Subscribe to UIActionService events for proper status updates
                if (_services.UIActionService != null)
                {
                    _services.UIActionService.StatusUpdated += OnStatusUpdated;
                    _services.UIActionService.EnhancedStatusUpdated += OnEnhancedStatusUpdated;
                    _services.UIActionService.BusyStateChanged += OnBusyStateChanged;
                }
            }
        }
    }
    
    // Event handlers for UIActionService events
    private void OnEnhancedStatusUpdated(StatusType statusType, string title, string message, string details, bool hasAction, string actionText, ICommand? actionCommand)
    {
        StatusType = statusType;
        StatusTitle = title;
        StatusMessage = message;
        StatusDetails = details;
        StatusHasAction = hasAction;
        StatusActionText = actionText;
        StatusActionCommand = actionCommand;
        
        // Set color for backward compatibility
        StatusMessageColor = statusType switch
        {
            StatusType.Success => Brushes.LightGreen,
            StatusType.Error => Brushes.OrangeRed,
            StatusType.Warning => Brushes.Orange,
            StatusType.Information => Brushes.White,
            StatusType.Processing => Brushes.LightBlue,
            _ => Brushes.White
        };
    }
    
    private void OnStatusUpdated(string message, IBrush color)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = message;
            StatusMessageColor = color;
        });
    }
    
    private void OnBusyStateChanged(bool isBusy)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = isBusy;
            CanCancel = isBusy;
        });
    }
    
    // Command implementations - using UIActionService pattern like TradeDataHub
    private async Task ExecuteExportToExcelCommandAsync()
    {
        try
        {
            if (Services?.UIActionService != null)
            {
                // Prepare filter data before calling service
                PrepareFilterWithDefaults();
                SetCurrentFilterInService();
                
                // Call UIActionService like TradeDataHub does
                await Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusMessage = "Service not initialized. Please restart the application.";
                    StatusMessageColor = Brushes.OrangeRed;
                });
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageColor = Brushes.OrangeRed;
            });
        }
    }
    
    private async Task ExecuteRunQueryCommandAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = "Query functionality not implemented yet";
            StatusMessageColor = Brushes.Orange;
        });
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
    
    private async Task ExecuteCancelImportCommandAsync()
    {
        if (Services?.UIActionService != null)
        {
            Services.UIActionService.HandleCancel();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = "Cancel operation not available";
                StatusMessageColor = Brushes.Orange;
            });
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
            ToMonth = $"{_toYear}{_toMonth:D2}"
        };
        
        // Set initial current mode
        _currentMode = "Export";
        
        StatusMessage = "Ready";
        StatusMessageColor = Brushes.White;
        
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
            StatusMessage = $"Failed to load database objects: {ex.Message}";
            StatusMessageColor = Brushes.OrangeRed;
        }
    }
    
    /// <summary>
    /// Updates database selections (views, stored procedures) based on the current application mode
    /// </summary>
    private void UpdateDatabaseSelectionsForMode()
    {
        if (Services == null)
        {
            StatusMessage = "Services not initialized";
            StatusMessageColor = Brushes.OrangeRed;
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
                    
                    StatusMessage = "Export mode: Database objects loaded";
                    StatusMessageColor = Brushes.White;
                }
                else
                {
                    StatusMessage = "Export validation service not available";
                    StatusMessageColor = Brushes.OrangeRed;
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
                    
                    StatusMessage = "Import mode: Database objects loaded";
                    StatusMessageColor = Brushes.White;
                }
                else
                {
                    StatusMessage = "Import validation service not available";
                    StatusMessageColor = Brushes.OrangeRed;
                }
            }
            
            // Update ExporterLabelText
            this.RaisePropertyChanged(nameof(ExporterLabelText));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update database selections: {ex.Message}";
            StatusMessageColor = Brushes.OrangeRed;
        }
    }
}
