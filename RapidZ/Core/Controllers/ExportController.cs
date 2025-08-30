using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using RapidZ.Core.Models;
using RapidZ.Core.Services;
using RapidZ.Features.Export;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Monitoring.Services;
using RapidZ.Features.Monitoring.Models;
using MonitoringLogLevel = RapidZ.Features.Monitoring.Models.LogLevel;
using RapidZ.Core.Logging;

namespace RapidZ.Core.Controllers
{
    /// <summary>
    /// Controller for export operations
    /// </summary>
    public class ExportController : IExportController
    {
        private readonly ExportExcelService _excelService;
        private readonly IValidationService _validationService;
        private readonly IResultProcessorService _resultProcessorService;
        private readonly MonitoringService _monitoringService;
        private readonly Dispatcher _dispatcher;

        public ExportController(
            ExportExcelService excelService, 
            IValidationService validationService,
            IResultProcessorService resultProcessorService,
            MonitoringService monitoringService)
        {
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _resultProcessorService = resultProcessorService ?? throw new ArgumentNullException(nameof(resultProcessorService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _dispatcher = Dispatcher.UIThread;
        }

        public async Task RunAsync(ExportInputs exportInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure)
        {
            // Validate using ValidationService
            var validationResult = _validationService.ValidateExportOperation(exportInputs, selectedView, selectedStoredProcedure);
            if (!validationResult.IsValid)
            {
                // For Avalonia, we'll update the monitoring service instead of showing MessageBox
                _monitoringService.SetError(validationResult.ErrorMessage, "Validation");
                return;
            }

            var ports = exportInputs.Ports;
            var hsCodes = exportInputs.HSCodes;
            var products = exportInputs.Products;
            var exporters = exportInputs.Exporters;
            var foreignCountries = exportInputs.ForeignCountries;
            var foreignNames = exportInputs.ForeignNames;
            var iecs = exportInputs.IECs;

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
                            foreach (var exporter in exporters)
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
                                            _resultProcessorService.UpdateProcessingStatus(counters.CombinationsProcessed, _monitoringService, "Export");

                                            try
                                            {
                                                // Step 1: Execute SP and get data reader with row count (single SP execution)
                                                var result = _excelService.CreateReport(
                                                    counters.CombinationsProcessed, 
                                                    exportInputs.FromMonth, 
                                                    exportInputs.ToMonth, 
                                                    hsCode, 
                                                    product, 
                                                    iec, 
                                                    exporter, 
                                                    country, 
                                                    name, 
                                                    port, 
                                                    cancellationToken,
                                                    selectedView,
                                                    selectedStoredProcedure);
                                                
                                                // Process the result using ResultProcessorService
                                                _resultProcessorService.ProcessExportResult(counters, result, counters.CombinationsProcessed, _monitoringService);
                                                
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
                                                var filterDetails = $"HSCode:{hsCode}, Product:{product}, IEC:{iec}, Exporter:{exporter}, Country:{country}, Name:{name}, Port:{port}";
                                                _resultProcessorService.HandleProcessingError(ex, counters.CombinationsProcessed, _monitoringService, "Export", filterDetails);
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
                _resultProcessorService.HandleCancellation(counters, _monitoringService, "Export");
                return;
            }

            // Log processing summary using ResultProcessorService
            _resultProcessorService.LogProcessingSummary(counters);

            // Generate completion summary using ResultProcessorService
            var summaryMessage = _resultProcessorService.GenerateCompletionSummary(counters, "Export");

            // Update monitoring service with completion message
            _monitoringService.SetInfo(summaryMessage, "Export");

            // Update final status
            _dispatcher.Invoke(() => _monitoringService.UpdateStatus(StatusType.Completed, GetStatusSummary(counters.FilesGenerated, counters.SkippedNoData, counters.SkippedRowLimit)));
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