using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using RapidZ.Core.Models;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for log parsing and monitoring
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Log monitoring and execution summary methods
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
}
