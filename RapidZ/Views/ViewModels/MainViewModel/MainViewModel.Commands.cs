using System;
using System.Windows.Input;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Threading;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for command declarations and setup
/// </summary>
public partial class MainViewModel
{
    // Commands - using simple command pattern to avoid ReactiveCommand threading issues
    public ICommand ExportToExcelCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    public ICommand CancelImportCommand { get; private set; } = null!;
    public ICommand RefreshExecutionSummaryCommand { get; private set; } = null!;
    
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
}
