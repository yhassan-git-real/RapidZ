using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using RapidZ.Models;
using RapidZ.Services;
using RapidZ.Helpers;

namespace RapidZ.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ConfigurationService _configService;
    private readonly ExportViewModel _exportViewModel;
    private readonly DatabaseService _databaseService;
    private readonly ExcelService _excelService;
    
    private string _statusMessage = string.Empty;
    private IBrush _statusMessageColor = Brushes.White;
    private bool _isBusy;
    private ConnectionInfo _connectionInfo = new();
    
    // Date properties
    private int _fromYear;
    private int _fromMonth;
    private int _toYear;
    private int _toMonth;

    public MainViewModel(
        ConfigurationService configService, 
        ExportViewModel exportViewModel,
        DatabaseService databaseService,
        ExcelService excelService)
    {
        _configService = configService;
        _exportViewModel = exportViewModel;
        _databaseService = databaseService;
        _excelService = excelService;
        
        // Initialize with current date
        var currentDate = DateTime.Now;
        _fromYear = currentDate.Year;
        _fromMonth = currentDate.Month;
        _toYear = currentDate.Year;
        _toMonth = currentDate.Month;
        
        ExportDataFilter = new ExportDataFilter
        {
            Mode = "Export" // Default mode
        };
        
        // Initialize commands
        RunQueryCommand = ReactiveCommand.CreateFromTask(ExecuteRunQueryCommandAsync);
        ExportToExcelCommand = ReactiveCommand.CreateFromTask(ExecuteExportToExcelCommandAsync);
        ClearFiltersCommand = ReactiveCommand.Create(ExecuteClearFiltersCommand);
        
        // Initialize connection info
        _connectionInfo = _databaseService.GetConnectionInfo();
        
        // Subscribe to database connection status changes
        _databaseService.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(DatabaseService.IsConnected) || 
                e.PropertyName == nameof(DatabaseService.ConnectionStatus))
            {
                UpdateConnectionInfo();
            }
        };
        
        // Initial connection check
        Task.Run(async () => 
        {
            await _databaseService.CheckConnectionAsync();
            UpdateConnectionInfo();
        });
    }
    
    private void UpdateConnectionInfo()
    {
        ConnectionInfo = _databaseService.GetConnectionInfo();
    }

    public ExportDataFilter ExportDataFilter { get; }
    
    public ICommand RunQueryCommand { get; }
    public ICommand ExportToExcelCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    
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
    
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    
    public ConnectionInfo ConnectionInfo
    {
        get => _connectionInfo;
        set => this.RaiseAndSetIfChanged(ref _connectionInfo, value);
    }
    

    
    // Date Properties
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
    
    private void UpdateFromMonthValue()
    {
        ExportDataFilter.FromMonth = $"{_fromYear}{_fromMonth:D2}";
    }
    
    private void UpdateToMonthValue()
    {
        ExportDataFilter.ToMonth = $"{_toYear}{_toMonth:D2}";
    }
    
    private async Task ExecuteRunQueryCommandAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Retrieving data...";
            StatusMessageColor = Brushes.White;
            
            // Validate input
            if (string.IsNullOrEmpty(ExportDataFilter.FromMonth) || string.IsNullOrEmpty(ExportDataFilter.ToMonth))
            {
                StatusMessage = "Please select date range";
                StatusMessageColor = Brushes.OrangeRed;
                return;
            }
            
            // Handle empty fields with wildcards
            PrepareFilterWithDefaults();
            
            // Execute query here
            var result = await _databaseService.ExecuteExportDataQuery(ExportDataFilter);
            
            // Update status
            StatusMessage = result.Count > 0 
                ? $"Data retrieved successfully. {result.Count} records found."
                : "No records found matching the criteria.";
            StatusMessageColor = result.Count > 0 ? Brushes.LightGreen : Brushes.Yellow;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusMessageColor = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExecuteExportToExcelCommandAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Generating report...";
            StatusMessageColor = Brushes.White;
            
            // Validate input
            if (string.IsNullOrEmpty(ExportDataFilter.FromMonth) || string.IsNullOrEmpty(ExportDataFilter.ToMonth))
            {
                StatusMessage = "Please select date range";
                StatusMessageColor = Brushes.OrangeRed;
                return;
            }
            
            // Handle empty fields with wildcards
            PrepareFilterWithDefaults();
            
            // First run the query
            await ExecuteRunQueryCommandAsync();
            
            // Pass to export view model for processing
            var result = await _exportViewModel.ProcessExportAsync(ExportDataFilter);
            
            StatusMessage = result ? "Report generated successfully" : "Report generation failed";
            StatusMessageColor = result ? Brushes.LightGreen : Brushes.OrangeRed;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusMessageColor = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ExecuteClearFiltersCommand()
    {
        // Clear all filter fields
        ExportDataFilter.HSCode = string.Empty;
        ExportDataFilter.Product = string.Empty;
        ExportDataFilter.Exporter = string.Empty;
        ExportDataFilter.IEC = string.Empty;
        ExportDataFilter.ForeignParty = string.Empty;
        ExportDataFilter.ForeignCountry = string.Empty;
        ExportDataFilter.Port = string.Empty;
        
        // Reset date to current
        var currentDate = DateTime.Now;
        FromYear = currentDate.Year;
        FromMonth = currentDate.Month;
        ToYear = currentDate.Year;
        ToMonth = currentDate.Month;
        
        // Reset status
        StatusMessage = "Form reset completed";
        StatusMessageColor = Brushes.White;
    }
    
    // Set default wildcards for empty fields
    private void PrepareFilterWithDefaults()
    {
        var defaultWildcard = _configService.AppSettings.ApplicationSettings.DefaultWildcard;
        
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
}
