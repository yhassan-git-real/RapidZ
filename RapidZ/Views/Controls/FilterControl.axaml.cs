using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RapidZ.Views.ViewModels;
using System;
using System.Threading;
using RapidZ.Core.Services;

namespace RapidZ.Views.Controls;

public partial class FilterControl : UserControl
{
    public FilterControl()
    {
        InitializeComponent();
    }

    private async void OnGenerateReportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnGenerateReportClick called!");
            
            // Get the MainWindow to access services
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            System.Diagnostics.Debug.WriteLine($"MainWindow found: {mainWindow != null}");
            
            if (mainWindow?.Services?.UIActionService != null)
            {
                // Set the current filter in the UIActionService
                if (DataContext is MainViewModel viewModel)
                {
                    if (mainWindow.Services.UIActionService is UIActionService uiActionService)
                    {
                        // Set the export filter directly on the UIActionService
                        uiActionService.SetCurrentExportFilter(viewModel.ExportDataFilter);
                        uiActionService.SetSelectedView(viewModel.SelectedView?.Name ?? "");
                        uiActionService.SetSelectedStoredProcedure(viewModel.SelectedStoredProcedure?.Name ?? "");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Calling UIActionService.HandleGenerateAsync...");
                await mainWindow.Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("UIActionService is null!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnGenerateReportClick: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnCancelClick called!");
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            mainWindow?.Services?.UIActionService?.HandleCancel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnCancelClick: {ex.Message}");
        }
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnResetClick called!");
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            mainWindow?.Services?.UIActionService?.HandleReset();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnResetClick: {ex.Message}");
        }
    }
}
