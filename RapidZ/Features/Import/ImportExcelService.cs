using System;
using System.Data;
using System.IO;
using System.Threading;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Logging;
using RapidZ.Core.Helpers;
using RapidZ.Core.Database;
using RapidZ.Core.Services;
using RapidZ.Core.Cancellation;
using RapidZ.Core.DataAccess;
using System.Drawing;
using System.Linq;

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
        private readonly ModuleLogger _logger;
        private readonly ImportSettings _settings;
        private readonly ImportExcelFormatSettings _formatSettings;
        
        // Public property to access import settings
        public ImportSettings ImportSettings => _settings;

        public ImportExcelService()
        {
            _logger = ModuleLoggerFactory.GetImportLogger();
            
            // Use static configuration cache methods like TradeDataHub
            _settings = ConfigurationCacheService.GetImportSettings();
            _formatSettings = ConfigurationCacheService.GetImportExcelFormatSettings();
        }

        public ImportExcelResult CreateReport(
            string fromMonth, string toMonth, string hsCode, string product, 
            string iec, string importer, string country, string name, string port,
            CancellationToken cancellationToken = default, string? viewName = null, 
            string? storedProcedureName = null)
        {
            var startTime = DateTime.Now;
            var result = new ImportExcelResult();
            string processId = _logger.GenerateProcessId();
            
            // Format parameters for logging
            string paramSummary = $"fromMonth:{fromMonth}, toMonth:{toMonth}, hsCode:{hsCode}, product:{product}, " +
                                  $"iec:{iec}, importer:{importer}, country:{country}, name:{name}, port:{port}";
            
            // Log process start with rich formatting
            _logger.LogProcessStart(_settings.Logging.OperationLabel, paramSummary, processId);

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
                        DateTime.Now - startTime, "Failed - Missing configuration", processId);
                    return result;
                }

                // Log step for database operation
                _logger.LogStep("Database", "Executing stored procedure", processId);
                
                // Get data using ImportDataAccess
                var dataAccess = new ImportDataAccess(_settings);
                // Get database data using a timer
                Tuple<SqlConnection, SqlDataReader, long> spData;
                using (var spTimer = _logger.StartTimer("Database", processId))
                {
                    spData = dataAccess.GetDataReader(
                        fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, 
                        cancellationToken, viewName, storedProcedureName);
                }
                
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
                        ModuleSkippedDatasetLogger.LogImportSkippedDataset(0, recordCount, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "ExcelRowLimit");
                        result.Success = false;
                        result.RowCount = recordCount;
                        result.SkipReason = "ExcelRowLimit";
                        _logger.LogSkipped($"{hsCode}_{fromMonth}-{toMonth}", recordCount, "Row limit exceeded", processId);
                        
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                            DateTime.Now - startTime, "Skipped - Row limit exceeded", processId);
                        return result;
                    }

                    // Check for no data
                    if (recordCount == 0)
                    {
                        ModuleSkippedDatasetLogger.LogImportSkippedDataset(0, 0, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "NoData");
                        result.Success = false;
                        result.SkipReason = "NoData";
                        _logger.LogSkipped($"{hsCode}_{fromMonth}-{toMonth}", 0, "No data", processId);
                        
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                            DateTime.Now - startTime, "Skipped - No data", processId);
                        return result;
                    }

                    // Generate file name
                    var fileName = Import_FileNameHelper.GenerateImportFileName(
                        fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, 
                        _settings.Files.FileSuffix);
                        
                    // Ensure output directory exists
                    if (!Directory.Exists(_settings.Files.OutputDirectory))
                    {
                        Directory.CreateDirectory(_settings.Files.OutputDirectory);
                        _logger.LogInfo($"Created output directory: {_settings.Files.OutputDirectory}", processId);
                    }
                    
                    var filePath = Path.Combine(_settings.Files.OutputDirectory, fileName);
                    
                    // Log Excel file creation start
                    _logger.LogExcelFileCreationStart(fileName, processId);
                    
                    cancellationToken.ThrowIfCancellationRequested();

                    // Use Using statement for the timer to properly dispose and log
                    using (var excelTimer = _logger.StartTimer("Excel creation", processId))
                    {
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
                            using (var saveTimer = _logger.StartTimer("File save", processId))
                            {
                                // For smaller files, use memory stream for better performance
                                if (recordCount < 10000)
                                {
                                    var fileBytes = package.GetAsByteArray();
                                    File.WriteAllBytes(filePath, fileBytes);
                                }
                                else
                                {
                                    // For larger files, save directly to avoid memory issues
                                    package.SaveAs(new FileInfo(filePath));
                                }
                                
                                // Timer will auto-stop on dispose
                            }
                            _logger.LogFileSave("Completed", excelTimer.Elapsed, processId);
                        }
                        
                        _logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
                    }

                    result.Success = true;
                    result.FileName = fileName;
                    result.RowCount = recordCount;
                    
                    // Log overall completion
                    _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                        DateTime.Now - startTime, $"Success - {fileName}", processId);
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.SkipReason = "Cancelled";
                _logger.LogWarning("Import operation was cancelled", processId);
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                    DateTime.Now - startTime, "Cancelled by user", processId);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.SkipReason = "Error";
                _logger.LogError($"Error in CreateReport: {ex.Message}", ex);
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, 
                    DateTime.Now - startTime, $"Failed - {ex.Message}", processId);
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

                cancellationToken.ThrowIfCancellationRequested();

                // Apply column-specific formatting
                foreach (var dateCol in _formatSettings.DateColumns)
                {
                    if (dateCol <= totalColumns)
                    {
                        var dateRange = worksheet.Cells[2, dateCol, totalRows, dateCol];
                        dateRange.Style.Numberformat.Format = _formatSettings.DateFormat;
                    }
                }

                foreach (var textCol in _formatSettings.TextColumns)
                {
                    if (textCol <= totalColumns)
                    {
                        var textRange = worksheet.Cells[2, textCol, totalRows, textCol];
                        textRange.Style.Numberformat.Format = "@"; // Text format
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Apply border to entire range
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                cancellationToken.ThrowIfCancellationRequested();

                // Format header row
                var headerRange = worksheet.Cells[1, 1, 1, totalColumns];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(_formatSettings.HeaderBackgroundColor));

                cancellationToken.ThrowIfCancellationRequested();

                // Auto-fit columns if enabled
                if (_formatSettings.AutoFitColumns)
                {
                    // Sample-based auto-fit for better performance on large datasets
                    int sampleRows = Math.Min(_formatSettings.AutoFitSampleRows, totalRows);
                    for (int col = 1; col <= totalColumns; col++)
                    {
                        var sampleRange = worksheet.Cells[1, col, sampleRows, col];
                        sampleRange.AutoFitColumns();
                    }
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