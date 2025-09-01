using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Logging.Services;
using RapidZ.Core.Helpers;
using RapidZ.Core.Database;
using RapidZ.Core.Services;
using RapidZ.Core.Cancellation;
using RapidZ.Core.DataAccess;
using System.Drawing;
using System.Linq;
using RapidZ.Config;

namespace RapidZ.Features.Import
{
    public class ImportExcelResult
    {
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public long RowCount { get; set; }
        public string? SkipReason { get; set; }
        public bool IsCancelled => SkipReason == "Cancelled";
    }

    public class ImportExcelService
    {
        private readonly RapidZ.Core.Logging.Abstractions.IModuleLogger _logger;
        private readonly ImportSettings _settings;
        private readonly ImportExcelFormatSettings _formatSettings;
        private List<string> _generatedFileNames = new List<string>();
        private Dictionary<string, List<string>> _parameterToFileMap = new Dictionary<string, List<string>>();
        
        // Public property to access import settings
        public ImportSettings ImportSettings => _settings;
        
        // Get a list of files generated for the current input parameters
        public List<string> GetGeneratedFileNames() => _generatedFileNames;
        
        // Clear the generated files list for the new operation
        public void ClearGeneratedFiles()
        {
            _generatedFileNames = new List<string>();
        }

        public ImportExcelService()
        {
            _logger = LoggerFactory.GetImportLogger();
            
            // Use static configuration cache methods like TradeDataHub
            _settings = ConfigurationCacheService.GetImportSettings();
            _formatSettings = ConfigurationCacheService.GetImportExcelFormatSettings();
        }

        public ImportExcelResult CreateReport(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string importer, string country, string name, string port,
            CancellationToken cancellationToken = default, string? viewName = null, 
            string? storedProcedureName = null, string? customOutputPath = null)
        {
            var result = new ImportExcelResult();
            string processId = _logger.GenerateProcessId();
            
            // Format parameters for logging
            string paramSummary = $"fromMonth:{fromMonth}, toMonth:{toMonth}, hsCode:{hsCode}, product:{product}, " +
                                  $"iec:{iec}, importer:{importer}, country:{country}, name:{name}, port:{port}";
            
            // Log process start with rich formatting
            _logger.LogProcessStart(_settings.Logging.OperationLabel, paramSummary, processId);

            var reportTimer = Stopwatch.StartNew();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Validate configuration
                if (string.IsNullOrEmpty(_settings.Database.StoredProcedureName))
                {
                    _logger.LogError("Import stored procedure name is not configured", processId);
                    result.Success = false;
                    result.SkipReason = "Configuration";
                    
                    _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                        reportTimer.Elapsed, "Failed - Missing configuration", processId);
                    return result;
                }

                _logger.LogStep("Database", "Executing stored procedure", processId);
                
                // Get data using ImportDataAccess
                var dataAccess = new ImportDataAccess(_settings);
                var spData = dataAccess.GetDataReader(
                    fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, 
                    cancellationToken, viewName, storedProcedureName);
                
                var connection = spData.Item1;
                var reader = spData.Item2;
                var recordCount = spData.Item3;
                
                _logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);

                using (connection)
                using (reader)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Check for data limit
                    if (recordCount > RapidZ.Core.Parameters.Import.ImportParameterHelper.MAX_EXCEL_ROWS)
                    {
                        var datasetLogger = LoggerFactory.GetDatasetLogger();
                datasetLogger.LogSkippedDataset(new RapidZ.Core.Logging.Models.SkippedDatasetInfo
                {
                    ModuleType = "Import",
                    CombinationNumber = 0,
                    RowCount = recordCount,
                    Reason = "RowLimit",
                    Parameters = new RapidZ.Core.Logging.Models.ProcessParameters
                    {
                        FromMonth = fromMonth,
                        ToMonth = toMonth,
                        HsCode = hsCode,
                        Product = product,
                        Iec = iec,
                        ExporterOrImporter = importer,
                         Country = country,
                         Name = name,
                        Port = port
                    }
                });
                        result.Success = false;
                        result.RowCount = recordCount;
                        result.SkipReason = "ExcelRowLimit";
                        _logger.LogSkipped($"{hsCode}_{fromMonth}-{toMonth}", recordCount, "Row limit exceeded", processId);
                        
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                            reportTimer.Elapsed, "Skipped - Row limit exceeded", processId);
                        return result;
                    }

                    // Check for no data
                    if (recordCount == 0)
                    {
                        var datasetLogger = LoggerFactory.GetDatasetLogger();
                datasetLogger.LogSkippedDataset(new RapidZ.Core.Logging.Models.SkippedDatasetInfo
                {
                    ModuleType = "Import",
                    CombinationNumber = 0,
                    RowCount = 0,
                    Reason = "NoData",
                    Parameters = new RapidZ.Core.Logging.Models.ProcessParameters
                    {
                        FromMonth = fromMonth,
                        ToMonth = toMonth,
                        HsCode = hsCode,
                        Product = product,
                        Iec = iec,
                        ExporterOrImporter = importer,
                         Country = country,
                         Name = name,
                        Port = port
                    }
                });
                        result.Success = false;
                        result.SkipReason = "NoData";
                        _logger.LogSkipped($"{hsCode}_{fromMonth}-{toMonth}", 0, "No data", processId);
                        
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                            reportTimer.Elapsed, "Skipped - No data", processId);
                        return result;
                    }

                    // Generate file name
                    var fileName = Import_FileNameHelper.GenerateImportFileName(
                        fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, 
                        _settings.Files.FileSuffix);
                        
                    // Use custom output path if provided, otherwise use default
                    string outputDir = customOutputPath ?? _settings.Files.OutputDirectory;
                    Directory.CreateDirectory(outputDir);
                    var filePath = Path.Combine(outputDir, fileName);
                    
                    _logger.LogExcelFileCreationStart(fileName, processId);
                    var excelTimer = Stopwatch.StartNew();

                    // Create Excel package
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add(_settings.Database.WorksheetName);

                        // Load data from reader
                        worksheet.Cells["A1"].LoadFromDataReader(reader, true);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Apply formatting
                        ApplyOptimizedFormatting(worksheet, (int)recordCount + 1, reader.FieldCount, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

                        // Save file
                        var saveTimer = Stopwatch.StartNew();
                        // For smaller files, use memory stream with pre-allocation for better performance
                        if (recordCount < 50000)
                        {
                            int estimatedSize = Math.Max(1024 * 1024, (int)recordCount * 100); // Estimate ~100 bytes per record minimum
                            using (var memoryStream = new MemoryStream(estimatedSize))
                            {
                                package.SaveAs(memoryStream);
                                var fileBytes = memoryStream.ToArray();
                                File.WriteAllBytes(filePath, fileBytes);
                            }
                        }
                        else
                        {
                            // For larger files, save directly to avoid memory issues
                            package.SaveAs(new FileInfo(filePath));
                        }
                        saveTimer.Stop();
                        
                        _logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
                        _logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
                        excelTimer.Stop();
                    }

                    result.Success = true;
                    result.FileName = fileName;
                    result.RowCount = recordCount;
                    
                    // Add to the list of generated files
                    _generatedFileNames.Add(fileName);
                    
                    // Log overall completion
                    _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                        reportTimer.Elapsed, $"Success - {fileName}", processId);
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.SkipReason = "Cancelled";
                _logger.LogWarning("Import operation was cancelled", processId);
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                    reportTimer.Elapsed, "Cancelled by user", processId);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.SkipReason = "Error";
                _logger.LogError($"Error in CreateReport: {ex.Message}", ex);
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                    reportTimer.Elapsed, $"Failed - {ex.Message}", processId);
            }

            return result;
        }

        private void ApplyOptimizedFormatting(ExcelWorksheet worksheet, int totalRows, int totalColumns, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Apply font settings to entire range
                var dataRange = worksheet.Cells[1, 1, totalRows, totalColumns];
                dataRange.Style.Font.Name = _formatSettings.FontName;
                dataRange.Style.Font.Size = _formatSettings.FontSize;
                dataRange.Style.WrapText = _formatSettings.WrapText;

                cancellationToken.ThrowIfCancellationRequested();

                // Apply column-specific formatting in bulk (optimized approach)
                foreach (int dateCol in _formatSettings.DateColumns)
                {
                    if (dateCol > 0 && dateCol <= totalColumns)
                        worksheet.Column(dateCol).Style.Numberformat.Format = _formatSettings.DateFormat;
                }

                foreach (int textCol in _formatSettings.TextColumns)
                {
                    if (textCol > 0 && textCol <= totalColumns)
                        worksheet.Column(textCol).Style.Numberformat.Format = "@"; // Text format
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Apply border to entire range using configurable border style
                var borderStyle = (_formatSettings.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
                    ? ExcelBorderStyle.None : ExcelBorderStyle.Thin;
                dataRange.Style.Border.Top.Style = borderStyle;
                dataRange.Style.Border.Bottom.Style = borderStyle;
                dataRange.Style.Border.Left.Style = borderStyle;
                dataRange.Style.Border.Right.Style = borderStyle;

                cancellationToken.ThrowIfCancellationRequested();

                // Format header row
                var headerRange = worksheet.Cells[1, 1, 1, totalColumns];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(_formatSettings.HeaderBackgroundColor));

                cancellationToken.ThrowIfCancellationRequested();

                // Auto-fit columns if enabled (optimized range-based approach with dynamic sample size)
                if (_formatSettings.AutoFitColumns)
                {
                    // Dynamic sample size based on dataset size for optimal performance
                    int sampleRowsConfig = totalRows > _formatSettings.LargeDatasetThreshold 
                        ? _formatSettings.AutoFitSampleRowsLarge 
                        : _formatSettings.AutoFitSampleRows;
                    int sampleRows = Math.Min(sampleRowsConfig, totalRows);
                    worksheet.Cells[1, 1, sampleRows, totalColumns].AutoFitColumns();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error applying formatting: {ex.Message}");
            }
        }
    }
}