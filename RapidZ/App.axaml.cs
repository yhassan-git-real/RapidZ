using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RapidZ.Services;
using RapidZ.ViewModels;
using RapidZ.Views;
using Serilog;

namespace RapidZ;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services
        var services = new ServiceCollection();
        
        // Configure Serilog
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("Logs/rapidz-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        services.AddLogging(builder =>
        {
            builder.AddSerilog(logger, dispose: true);
        });
        
        // Register services
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<ExcelService>();
        
        // Register view models
        services.AddSingleton<ExportViewModel>();
        services.AddSingleton<MainViewModel>();
        
        // Register views
        services.AddSingleton<MainWindow>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Get the main window from the service provider
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
