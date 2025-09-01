using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RapidZ.Core.Services;
using RapidZ.Views.ViewModels;
using RapidZ.Views;
using RapidZ.Core.Controllers;
using RapidZ.Features.Import;
using RapidZ.Core.Validation;
using RapidZ.Core.Logging;
using RapidZ.Core.DataAccess;
using RapidZ.Features.Export;
using RapidZ.Features.Import.Services;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Monitoring.Services;
using OfficeOpenXml; // EPPlus license context
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RapidZ;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // EPPlus license context (non-commercial as per plan)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services
        var services = new ServiceCollection();
        
        // Add basic console logging (no file logging)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        
        // Register core services
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<DatabaseService>();
        
        // Register configuration settings
        services.AddSingleton<ExportSettings>(provider => 
        {
            var exportSettingsRoot = ConfigurationCacheService.Instance.GetConfiguration<ExportSettingsRoot>(
                "Config/export.appsettings.json");
            return exportSettingsRoot.ExportSettings;
        });
        
        services.AddSingleton<ImportSettings>(provider => 
        {
            var importSettingsRoot = ConfigurationCacheService.Instance.GetConfiguration<ImportSettingsRoot>(
                "Config/import.appsettings.json");
            return importSettingsRoot.ImportSettings;
        });
        
        // Register validation services
        services.AddSingleton<IParameterValidator, ParameterValidator>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IResultProcessorService, ResultProcessorService>();
        
        // Register monitoring service (without real-time UI)
        services.AddSingleton<MonitoringService>();
        
        // Register Export services
        services.AddSingleton<ExportDataAccess>();
        services.AddSingleton<ExportExcelService>();
        services.AddSingleton<ExportObjectValidationService>();
        
        // Register Import services
        services.AddSingleton<ImportExcelService>();
        services.AddSingleton<ImportObjectValidationService>();
        
        // Register controllers
        services.AddSingleton<ExportController>();
        services.AddSingleton<ImportController>();
        services.AddSingleton<IExportController, ExportController>(provider => provider.GetRequiredService<ExportController>());
        services.AddSingleton<IImportController, ImportController>(provider => provider.GetRequiredService<ImportController>());
        
        // Register validation services
        services.AddSingleton<PathValidationService>();
        
        // Register view models
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<MainViewModel>();
        
        // Register views
        services.AddSingleton<MainWindow>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Perform initial database connection test asynchronously (non-blocking)
        _ = Task.Run(async () => await PerformStartupConnectionTestAsync());
        
        // Get the main window from the service provider
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create a unified ServiceContainer for the entire application
            var serviceContainer = new ServiceContainer();
            serviceContainer.InitializeServices();
            
            var mainWindow = new MainWindow(serviceContainer);
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            
            // Set the ServiceContainer on the MainViewModel
            mainViewModel.Services = serviceContainer;
            
            // Make sure UIActionService is initialized with the MainWindow
            if (serviceContainer.UIActionService is UIActionService uiActionService)
            {
                uiActionService.Initialize(mainWindow);
            }
            
            // Set the parent window for DialogService
            DialogService.SetParentWindow(mainWindow);
            
            mainWindow.DataContext = mainViewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Performs initial database connection test during application startup (async, non-blocking)
    /// </summary>
    private async Task PerformStartupConnectionTestAsync()
    {
        try
        {
            // Small delay to allow UI to initialize first
            await Task.Delay(100);
            
            // Get the database connection service instance
            var connectionService = DatabaseConnectionService.Instance;
            
            // Perform startup connection test with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await connectionService.TestConnectionOnStartupAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't prevent application startup
            System.Diagnostics.Debug.WriteLine($"Startup connection test failed: {ex.Message}");
        }
    }
}
