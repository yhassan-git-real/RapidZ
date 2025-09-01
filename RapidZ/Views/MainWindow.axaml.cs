using Avalonia.Controls;
using RapidZ.Core.Services;
using RapidZ.Views.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace RapidZ.Views;

public partial class MainWindow : Window
{
    private ServiceContainer? _services;

    // Parameterless constructor for XAML compatibility
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(ServiceContainer services)
    {
        InitializeComponent();
        
        // Use the service container injected from DI
        _services = services;
        
        // Initialize UIActionService with this window
        if (_services.UIActionService is UIActionService uiActionService)
        {
            uiActionService.Initialize(this);
        }
        
        // Note: DataContext will be set by App.axaml.cs through DI
        // This ensures proper dependency injection of controllers
    }
    
    // Expose services for UI access
    public ServiceContainer? Services => _services;
}
