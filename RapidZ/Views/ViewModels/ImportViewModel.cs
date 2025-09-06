using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using RapidZ.Core.Models;
using RapidZ.Features.Import;
using RapidZ.Core.Logging.Services;

namespace RapidZ.Views.ViewModels;

public class ImportViewModel : ReactiveObject
{
    private readonly ImportExcelService _importExcelService;
    private readonly RapidZ.Core.Logging.Abstractions.IModuleLogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Progress and status properties
    private string _statusMessage = "Ready";
    private bool _isImporting = false;
    private double _progressPercentage = 0;
    private bool _canCancel = false;
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
    
    public bool IsImporting
    {
        get => _isImporting;
        set => this.RaiseAndSetIfChanged(ref _isImporting, value);
    }
    
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
    }
    
    public bool CanCancel
    {
        get => _canCancel;
        set => this.RaiseAndSetIfChanged(ref _canCancel, value);
    }
    
    public ImportViewModel(ImportExcelService importExcelService)
    {
        _importExcelService = importExcelService ?? throw new ArgumentNullException(nameof(importExcelService));
        _logger = LoggerFactory.GetImportLogger();
    }
    
    /// <summary>
    /// Generates import Excel report for single parameter combination
    /// </summary>
    public async Task<ImportExcelResult> GenerateImportReportAsync(
        string fromMonth, string toMonth, string hsCode, 
        string product, string iec, string importer, 
        string country, string name, string port)
    {
        return await GenerateImportReportAsync(
            fromMonth, toMonth, hsCode, product, iec, importer, 
            country, name, port, CancellationToken.None);
    }
    
    /// <summary>
    /// Generates import Excel report for single parameter combination with cancellation support
    /// </summary>
    public async Task<ImportExcelResult> GenerateImportReportAsync(
        string fromMonth, string toMonth, string hsCode, 
        string product, string iec, string importer, 
        string country, string name, string port,
        CancellationToken cancellationToken)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsImporting = true;
                CanCancel = true;
                StatusMessage = "Generating import report...";
                ProgressPercentage = 0;
            });

            _logger.LogInfo($"Starting import report generation for {fromMonth} to {toMonth}");
            
            var result = await Task.Run(() => 
                _importExcelService.CreateReport(
                    fromMonth, toMonth, hsCode, product, iec, importer, 
                    country, name, port, cancellationToken), cancellationToken);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = result.Success ? 
                    $"Report generated successfully. Records: {result.RowCount:N0}" : 
                    result.SkipReason ?? "Report generation failed";
                ProgressPercentage = 100;
            });

            _logger.LogInfo($"Import report generation completed. Success: {result.Success}");
            return result;
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = "Import operation cancelled";
                ProgressPercentage = 0;
            });
            
            _logger.LogWarning("Import report generation was cancelled");
            return new ImportExcelResult
            {
                Success = false,
                SkipReason = "Cancelled"
            };
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
                ProgressPercentage = 0;
            });
            
            _logger.LogError($"Error in import report generation: {ex.Message}", ex);
            return new ImportExcelResult
            {
                Success = false,
                SkipReason = "Error"
            };
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsImporting = false;
                CanCancel = false;
            });
        }
    }
    
    /// <summary>
    /// Generates import Excel reports for multiple parameter combinations
    /// </summary>
    public async Task<List<ImportExcelResult>> GenerateImportReportsAsync(
        ImportInputs importInputs, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportExcelResult>();
        
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsImporting = true;
                CanCancel = true;
                StatusMessage = "Processing import parameters...";
                ProgressPercentage = 0;
            });

            _logger.LogInfo("Starting batch import report generation");
            
            // Calculate total combinations
            var totalCombinations = importInputs.Ports.Count * 
                                  importInputs.HSCodes.Count * 
                                  importInputs.Products.Count * 
                                  importInputs.Importers.Count * 
                                  importInputs.IECs.Count * 
                                  importInputs.ForeignCountries.Count * 
                                  importInputs.ForeignNames.Count;
            
            var processedCount = 0;
            
            // Generate reports for all combinations
            foreach (var port in importInputs.Ports)
            {
                foreach (var hsCode in importInputs.HSCodes)
                {
                    foreach (var product in importInputs.Products)
                    {
                        foreach (var importer in importInputs.Importers)
                        {
                            foreach (var iec in importInputs.IECs)
                            {
                                foreach (var country in importInputs.ForeignCountries)
                                {
                                    foreach (var name in importInputs.ForeignNames)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();
                                        
                                        var result = await GenerateImportReportAsync(
                                            importInputs.FromMonth, importInputs.ToMonth,
                                            hsCode, product, iec, importer, 
                                            country, name, port, cancellationToken);
                                        
                                        results.Add(result);
                                        processedCount++;
                                        
                                        var progress = (double)processedCount / totalCombinations * 100;
                                        await Dispatcher.UIThread.InvokeAsync(() =>
                                        {
                                            ProgressPercentage = progress;
                                            StatusMessage = $"Processed {processedCount} of {totalCombinations} combinations";
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            var successCount = results.Count(r => r.Success);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Batch processing completed. {successCount} of {results.Count} reports generated successfully";
                ProgressPercentage = 100;
            });
            
            _logger.LogInfo($"Batch import report generation completed. Success: {successCount}/{results.Count}");
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = "Batch import operation cancelled";
            });
            
            _logger.LogWarning("Batch import report generation was cancelled");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Batch processing error: {ex.Message}";
            });
            
            _logger.LogError($"Error in batch import report generation: {ex.Message}", ex);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsImporting = false;
                CanCancel = false;
            });
        }
        
        return results;
    }
    
    /// <summary>
    /// Cancels the current import operation
    /// </summary>
    public void CancelImport()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInfo("Import cancellation requested");
        }
    }
    
    /// <summary>
    /// Starts a new cancellation token source for import operations
    /// </summary>
    public CancellationToken GetCancellationToken()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        return _cancellationTokenSource.Token;
    }
}