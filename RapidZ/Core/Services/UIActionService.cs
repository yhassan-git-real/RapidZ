using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using RapidZ.Core.Controllers;
using RapidZ.Core.Models;
using RapidZ.Core.Services;
using RapidZ.Views.Models;

namespace RapidZ.Core.Services
{
    public interface IUIActionService
    {
        Task HandleGenerateAsync(CancellationToken cancellationToken);
        void HandleCancel();
        void HandleReset();
        void Initialize(Window window);
        void SetServiceContainer(ServiceContainer serviceContainer);
        void SetCurrentExportFilter(ExportDataFilter filter);
        void SetSelectedView(string viewName);
        void SetSelectedStoredProcedure(string spName);
        
        void UpdateStatus(StatusType type, string title, string message, string details = "", bool hasAction = false, string actionText = "", ICommand? actionCommand = null);
        Task ShowNotificationAsync(StatusType type, string title, string message, string details = "");
        
        event Action<string, IBrush>? StatusUpdated;
        event Action<StatusType, string, string, string, bool, string, ICommand?>? EnhancedStatusUpdated;
        event Action<bool>? BusyStateChanged;
    }

    public class UIActionService : IUIActionService
    {
        private readonly ExportController _exportController;
        private readonly ImportController _importController;
        private readonly IResultProcessorService _resultProcessorService;
        private Window? _mainWindow;
        private CancellationTokenSource? _currentCancellationSource;
        private ServiceContainer? _serviceContainer;

        // Current filter data
        private ExportDataFilter? _currentExportFilter;
        private string _selectedView = "";
        private string _selectedStoredProcedure = "";

        // Status update delegates
        public event Action<string, IBrush>? StatusUpdated;
        public event Action<StatusType, string, string, string, bool, string, ICommand?>? EnhancedStatusUpdated;
        public event Action<bool>? BusyStateChanged;

        public UIActionService(
            ExportController exportController, 
            ImportController importController,
            IResultProcessorService resultProcessorService)
        {
            _exportController = exportController;
            _importController = importController;
            _resultProcessorService = resultProcessorService;
        }

        public void Initialize(Window window)
        {
            _mainWindow = window;
        }

        public void SetServiceContainer(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public void SetCurrentExportFilter(ExportDataFilter filter)
        {
            _currentExportFilter = filter;
        }

        public void SetSelectedView(string viewName)
        {
            _selectedView = viewName;
        }

        public void SetSelectedStoredProcedure(string spName)
        {
            _selectedStoredProcedure = spName;
        }

        public async Task HandleGenerateAsync(CancellationToken cancellationToken)
        {
            try
            {
                _currentCancellationSource?.Dispose();
                _currentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Update UI on UI thread to show processing state
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Set busy state first
                    BusyStateChanged?.Invoke(true);
                    
                    // Update status panel with processing information
                    UpdateStatus(
                    StatusType.Processing,
                    "Running",
                    "Generating report...",
                    "Please wait while the report is being generated."
                );
                });

                // Get the current filter from user input
                var exportFilter = GetCurrentExportFilter();
                
                // Validate that we have a filter at all
                if (exportFilter == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateStatus(
                            StatusType.Error,
                            "Error",
                            "No filter data provided",
                            "Please fill in the required fields and try again."
                        );
                    });
                    return;
                }
                
                // Prepare filter with defaults for any empty fields
                PrepareFilterWithDefaults(exportFilter);
                
                // Validate required input
                if (string.IsNullOrEmpty(exportFilter.FromMonth) || string.IsNullOrEmpty(exportFilter.ToMonth))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UpdateStatus(
                            StatusType.Error,
                            "Validation Error",
                            "Please select date range",
                            "From Month and To Month are required fields."
                        );
                    });
                    return;
                }

                // Process based on mode - following TradeDataHub pattern exactly
                if (exportFilter?.Mode == "Import")
                {
                    // Import mode - use ImportController
                    var importInputs = ConvertToImportInputs(exportFilter);
                    await _importController.RunAsync(importInputs, _currentCancellationSource.Token, _selectedView, _selectedStoredProcedure);
                    
                    // Get the counters from ResultProcessorService to generate completion summary
                    var counters = _resultProcessorService.GetCurrentCounters();
                    var summaryMessage = _resultProcessorService.GenerateCompletionSummary(counters, "Import");
                    
                    // Format the message for both status update and notification
                    var title = "Import Complete";
                    var message = counters.FilesGenerated == 0
                        ? "Import completed with no files generated."
                        : $"Import completed successfully. Files Generated: {counters.FilesGenerated}";
                    
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        // Update the status panel
                        UpdateStatus(
                            StatusType.Success,
                            title,
                            message,
                            summaryMessage
                        );
                        
                        // Also show a notification popup with detailed statistics
                        await ShowNotificationAsync(
                            StatusType.Success,
                            title,
                            message,
                            summaryMessage
                        );
                        
                        // Show a message box with statistics like TradeDataHub does
                        await ShowMessageBoxAsync(
                            title,
                            summaryMessage
                        );
                    });
                }
                else
                {
                    // Export mode - use ExportController
                    var exportInputs = ConvertToExportInputs(exportFilter!);
                    await _exportController.RunAsync(exportInputs, _currentCancellationSource.Token, _selectedView, _selectedStoredProcedure);
                    
                    // Get the counters from ResultProcessorService to generate completion summary
                    var counters = _resultProcessorService.GetCurrentCounters();
                    var summaryMessage = _resultProcessorService.GenerateCompletionSummary(counters, "Export");
                    
                    // Format the message for both status update and notification
                    var title = "Export Complete";
                    var message = counters.FilesGenerated == 0
                        ? "Export completed with no files generated."
                        : $"Export completed successfully. Files Generated: {counters.FilesGenerated}";
                    
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        // Update the status panel
                        UpdateStatus(
                            StatusType.Success,
                            title,
                            message,
                            summaryMessage
                        );
                        
                        // Show a notification popup with detailed statistics
                        await ShowNotificationAsync(
                            StatusType.Success,
                            title,
                            message,
                            summaryMessage
                        );
                    });
                }
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateStatus(
                        StatusType.Warning,
                        "Operation Cancelled",
                        "Operation cancelled by user",
                        "The operation was cancelled before completion."
                    );
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    UpdateStatus(
                        StatusType.Error,
                        "Error Occurred",
                        $"Error: {ex.Message}",
                        ex.StackTrace ?? "No stack trace available"
                    );
                    
                    // Also show a notification popup for errors
                    await ShowNotificationAsync(
                        StatusType.Error,
                        "Error Occurred",
                        $"Error: {ex.Message}",
                        ex.StackTrace ?? "No stack trace available"
                    );
                });
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    BusyStateChanged?.Invoke(false);
                });

                _currentCancellationSource?.Dispose();
                _currentCancellationSource = null;
            }
        }

        public void HandleCancel()
        {
            _currentCancellationSource?.Cancel();
        }

        public void HandleReset()
        {
            // Reset filter data
            if (_currentExportFilter != null)
            {
                _currentExportFilter.HSCode = string.Empty;
                _currentExportFilter.Product = string.Empty;
                _currentExportFilter.Exporter = string.Empty;
                _currentExportFilter.IEC = string.Empty;
                _currentExportFilter.ForeignParty = string.Empty;
                _currentExportFilter.ForeignCountry = string.Empty;
                _currentExportFilter.Port = string.Empty;
                
                var currentDate = DateTime.Now;
                _currentExportFilter.FromMonth = $"{currentDate.Year}{currentDate.Month:D2}";
                _currentExportFilter.ToMonth = $"{currentDate.Year}{currentDate.Month:D2}";
            }
            
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatus(
                    StatusType.Information,
                    "Reset Complete",
                    "Form reset completed",
                    "All form fields have been reset to their default values."
                );
            });
        }

        private ExportDataFilter? GetCurrentExportFilter()
        {
            // Return the current filter data set by the view model
            // Do not create a new filter with hardcoded values if none exists
            // This ensures we're fully dependent on user input
            return _currentExportFilter;
        }
        
        private void PrepareFilterWithDefaults(ExportDataFilter? filter)
        {
            if (filter == null) return;
            
            // SQL wildcard character for matching any string
            var defaultWildcard = "%";
            
            // Only apply defaults to empty fields
            // This ensures we use user input when available
            if (string.IsNullOrWhiteSpace(filter.HSCode))
                filter.HSCode = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.Product))
                filter.Product = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.Exporter))
                filter.Exporter = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.IEC))
                filter.IEC = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.ForeignParty))
                filter.ForeignParty = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.ForeignCountry))
                filter.ForeignCountry = defaultWildcard;
                
            if (string.IsNullOrWhiteSpace(filter.Port))
                filter.Port = defaultWildcard;
        }

        private ImportInputs ConvertToImportInputs(ExportDataFilter filter)
        {
            // When in Import mode, the "Exporter" field is actually used for importers
            // This is because we're reusing the ExportDataFilter class for both modes
            return new ImportInputs(
                filter.FromMonth,
                filter.ToMonth,
                SplitAndTrim(filter.Port),
                SplitAndTrim(filter.HSCode),
                SplitAndTrim(filter.Product),
                SplitAndTrim(filter.Exporter),  // This is actually importers in Import mode
                SplitAndTrim(filter.IEC),
                SplitAndTrim(filter.ForeignCountry),
                SplitAndTrim(filter.ForeignParty)
            );
        }

        private ExportInputs ConvertToExportInputs(ExportDataFilter filter)
        {
            return new ExportInputs(
                filter.FromMonth,
                filter.ToMonth,
                SplitAndTrim(filter.Port),
                SplitAndTrim(filter.HSCode),
                SplitAndTrim(filter.Product),
                SplitAndTrim(filter.Exporter),
                SplitAndTrim(filter.IEC),
                SplitAndTrim(filter.ForeignCountry),
                SplitAndTrim(filter.ForeignParty)
            );
        }

        private List<string> SplitAndTrim(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string> { "%" };
                
            // Process comma-separated values and trim each one
            return input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .ToList();
        }
        
        /// <summary>
        /// Updates the status panel with enhanced information
        /// </summary>
        public void UpdateStatus(StatusType type, string title, string message, string details = "", bool hasAction = false, string actionText = "", ICommand? actionCommand = null)
        {
            // Update status with enhanced information
            EnhancedStatusUpdated?.Invoke(type, title, message, details, hasAction, actionText, actionCommand);
            
            // For backward compatibility, also update the basic status
            IBrush color = type switch
            {
                StatusType.Success => Brushes.LightGreen,
                StatusType.Error => Brushes.OrangeRed,
                StatusType.Warning => Brushes.Orange,
                StatusType.Information => Brushes.White,
                StatusType.Processing => Brushes.LightBlue,
                _ => Brushes.White
            };
            
            StatusUpdated?.Invoke(message, color);
        }
        
        /// <summary>
        /// Shows a notification popup with enhanced information
        /// </summary>
        public async Task ShowNotificationAsync(StatusType type, string title, string message, string details = "")
        {
            switch (type)
            {
                case StatusType.Success:
                    await DialogService.ShowSuccessAsync(message, title, details);
                    break;
                case StatusType.Error:
                    await DialogService.ShowErrorAsync(message, title, details);
                    break;
                case StatusType.Warning:
                    await DialogService.ShowWarningAsync(message, title, details);
                    break;
                case StatusType.Information:
                case StatusType.Processing:
                default:
                    await DialogService.ShowInfoAsync(message, title, details);
                    break;
            }
        }

        /// <summary>
        /// Shows a message box in Avalonia for operation completion
        /// </summary>
        /// <param name="title">Title of the message box</param>
        /// <param name="message">Message content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private Task ShowMessageBoxAsync(string title, string message)
        {
            // Use our existing DialogService with Success message type
            // The parameters are in the order (message, title, details) - we need to make sure the message contains the statistics
            return DialogService.ShowSuccessAsync(message, title);
        }
    }
}
