using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using RapidZ.Core.Models;
using RapidZ.Core.Services;
using RapidZ.Features.Import;
using RapidZ.Features.Import.Services;
using RapidZ.Features.Monitoring.Services;
using RapidZ.Features.Monitoring.Models;
using RapidZ.Core.Logging;
using MonitoringLogLevel = RapidZ.Features.Monitoring.Models.LogLevel;

namespace RapidZ.Core.Controllers
{
    /// <summary>
    /// Controller for import operations
    /// </summary>
    public class ImportController : IImportController
    {
        private readonly ImportExcelService _importExcelService;
        private readonly IValidationService _validationService;
        private readonly IResultProcessorService _resultProcessorService;
        private readonly MonitoringService _monitoringService;
        private readonly Dispatcher _dispatcher;

        public ImportController(
            ImportExcelService importExcelService,
            IValidationService validationService,
            IResultProcessorService resultProcessorService,
            MonitoringService monitoringService)
        {
            _importExcelService = importExcelService ?? throw new ArgumentNullException(nameof(importExcelService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _resultProcessorService = resultProcessorService ?? throw new ArgumentNullException(nameof(resultProcessorService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _dispatcher = Dispatcher.UIThread;
        }

        public async Task RunAsync(ImportInputs importInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure, string? customOutputPath = null)
        {
            // Track start time for performance metrics
            var startTime = DateTime.Now;
            
            // Clear any previously tracked files
            _importExcelService.ClearGeneratedFiles();
            
            try
            {
                _monitoringService.SetInfo("Starting import operation...", "Import");
                _monitoringService.SetInfo($"Import parameters: FromMonth={importInputs.FromMonth}, ToMonth={importInputs.ToMonth}, View={selectedView}, SP={selectedStoredProcedure}", "Debug");
                
                // Log the parameters for debugging
                if (importInputs.Importers != null && importInputs.Importers.Count > 0)
                {
                    _monitoringService.SetInfo($"Importers: {string.Join(", ", importInputs.Importers)}", "Debug");
                }
                
                // Validate using ValidationService
                var validationResult = _validationService.ValidateImportOperation(importInputs, selectedView, selectedStoredProcedure);
                if (!validationResult.IsValid)
                {
                    // For Avalonia, we'll update the monitoring service instead of showing MessageBox
                    _monitoringService.SetError(validationResult.ErrorMessage, "Validation");
                    return;
                }
            }
            catch (Exception ex)
            {
                _monitoringService.SetError($"Error initializing import: {ex.Message}", "Import");
                return;
            }

            // Make sure we have non-null collections
            var ports = importInputs.Ports ?? new List<string>() { "%" };
            var hsCodes = importInputs.HSCodes ?? new List<string>() { "%" };
            var products = importInputs.Products ?? new List<string>() { "%" };
            var importers = importInputs.Importers ?? new List<string>() { "%" };
            var foreignCountries = importInputs.ForeignCountries ?? new List<string>() { "%" };
            var foreignNames = importInputs.ForeignNames ?? new List<string>() { "%" };
            var iecs = importInputs.IECs ?? new List<string>() { "%" };

            // Initialize processing counters
            var counters = _resultProcessorService.InitializeCounters();

            await Task.Run(() =>
            {
                foreach (var port in ports)
                {
                    foreach (var hsCode in hsCodes)
                    {
                        foreach (var product in products)
                        {
                            foreach (var importer in importers)
                            {
                                foreach (var iec in iecs)
                                {
                                    foreach (var country in foreignCountries)
                                    {
                                        foreach (var name in foreignNames)
                                        {
                                            // Check for cancellation at the start of each combination
                                            if (cancellationToken.IsCancellationRequested)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();
                                            }

                                            counters.CombinationsProcessed++;
                                            _resultProcessorService.UpdateProcessingStatus(counters.CombinationsProcessed, _monitoringService, "Import");

                                            try
                                            {
                                                // Step 1: Execute SP and get data reader with row count (single SP execution)
                                                var result = _importExcelService.CreateReport(
                                                    importInputs.FromMonth, 
                                                    importInputs.ToMonth, 
                                                    hsCode, 
                                                    product, 
                                                    iec, 
                                                    importer, 
                                                    country, 
                                                    name, 
                                                    port, 
                                                    cancellationToken,
                                                    selectedView,
                                                    selectedStoredProcedure,
                                                    customOutputPath);
                                                
                                                // Process the result using ResultProcessorService
                                                _resultProcessorService.ProcessImportResult(counters, result, counters.CombinationsProcessed, _monitoringService);
                                                
                                                if (result.IsCancelled)
                                                {
                                                    cancellationToken.ThrowIfCancellationRequested();
                                                }
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                counters.CancelledCombinations++;
                                                throw; // Re-throw to exit the loops
                                            }
                                            catch (Exception ex)
                                            {
                                                // Handle error using ResultProcessorService
                                                var filterDetails = $"HSCode:{hsCode}, Product:{product}, IEC:{iec}, Importer:{importer}, Country:{country}, Name:{name}, Port:{port}";
                                                _resultProcessorService.HandleProcessingError(ex, counters.CombinationsProcessed, _monitoringService, "Import", filterDetails);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }, cancellationToken);

            // Check if operation was cancelled
            if (cancellationToken.IsCancellationRequested)
            {
                _resultProcessorService.HandleCancellation(counters, _monitoringService, "Import");
                return;
            }

            // Log processing summary using ResultProcessorService
            _resultProcessorService.LogProcessingSummary(counters);

            // Generate completion summary using ResultProcessorService
            var summaryMessage = _resultProcessorService.GenerateCompletionSummary(counters, "Import");

            // Update monitoring service with completion message
            _monitoringService.SetInfo(summaryMessage, "Import");

            // Update final status
            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, GetStatusSummary(counters.FilesGenerated, counters.SkippedNoData, counters.SkippedRowLimit)));
            
            // Show processing complete dialog with additional information
            var processingTime = DateTime.Now - startTime;
            _resultProcessorService.ShowProcessingCompleteDialog(counters, "Import", processingTime, _importExcelService.GetGeneratedFileNames());
        }

        private string GetStatusSummary(int filesGenerated, int skippedNoData, int skippedRowLimit)
        {
            if (filesGenerated == 0)
            {
                if (skippedNoData > 0 && skippedRowLimit == 0)
                    return "Complete: No files generated - all combinations had no data";
                else if (skippedRowLimit > 0 && skippedNoData == 0)
                    return "Complete: No files generated - all combinations exceeded row limits";
                else if (skippedNoData > 0 && skippedRowLimit > 0)
                    return $"Complete: No files generated - {skippedNoData} no data, {skippedRowLimit} over limits";
                else
                    return "Complete: No files generated";
            }
            else
            {
                var totalSkipped = skippedNoData + skippedRowLimit;
                if (totalSkipped == 0)
                    return $"Complete: {filesGenerated} files generated successfully";
                else
                    return $"Complete: {filesGenerated} files, {totalSkipped} skipped ({skippedNoData} no data, {skippedRowLimit} over limits)";
            }
        }
    }
}