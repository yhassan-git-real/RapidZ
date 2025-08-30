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
                });

                // Get the current filter from user input
                var exportFilter = GetCurrentExportFilter();
                
                // Validate that we have a filter at all
                if (exportFilter == null)
                {
                    // No filter data provided - operation cannot continue
                    return;
                }
                
                // Prepare filter with defaults for any empty fields
                PrepareFilterWithDefaults(exportFilter);
                
                // Validate required input
                if (string.IsNullOrEmpty(exportFilter.FromMonth) || string.IsNullOrEmpty(exportFilter.ToMonth))
                {
                    // Date range validation failed - operation cannot continue
                    return;
                }

                // Process based on mode - following TradeDataHub pattern exactly
                if (exportFilter?.Mode == "Import")
                {
                    // Import mode - use ImportController
                    var importInputs = ConvertToImportInputs(exportFilter);
                    await _importController.RunAsync(importInputs, _currentCancellationSource.Token, _selectedView, _selectedStoredProcedure);
                }
                else
                {
                    // Export mode - use ExportController
                    var exportInputs = ConvertToExportInputs(exportFilter!);
                    await _exportController.RunAsync(exportInputs, _currentCancellationSource.Token, _selectedView, _selectedStoredProcedure);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled 
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in operation: {ex.Message}");
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
                
                Console.WriteLine("Reset Complete: Form reset completed. All form fields have been reset to their default values.");
            });
        }

        private ExportDataFilter? GetCurrentExportFilter()
        {
            
            return _currentExportFilter;
        }
        
        private void PrepareFilterWithDefaults(ExportDataFilter? filter)
        {
            if (filter == null) return;
            
            // SQL wildcard character for matching any string
            var defaultWildcard = "%";
            
            
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
    }
}
