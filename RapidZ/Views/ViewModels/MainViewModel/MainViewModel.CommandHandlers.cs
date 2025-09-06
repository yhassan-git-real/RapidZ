using System;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;
using RapidZ.Views.Models;
using RapidZ.Core.Models;
using RapidZ.Core.Services;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Import.Services;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for command execution handlers
/// </summary>
public partial class MainViewModel
{
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
}
