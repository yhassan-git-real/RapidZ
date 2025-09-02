using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RapidZ.Config;
using RapidZ.Core.DataAccess;
using RapidZ.Core.Database;
using RapidZ.Core.Parameters.Export;
using RapidZ.Core.Logging.Services;
using RapidZ.Core.Services;
using RapidZ.Core.Helpers;
using System.Threading;
using RapidZ.Core.Cancellation;
using RapidZ.Core.Models;
using RapidZ.Features.Export;
using RapidZ.Features.Export.Services;

namespace RapidZ.Features.Export;

public enum SkipReason
{
	None,
	NoData,
	ExcelRowLimit,
	Cancelled
}

public class ExcelResult
{
	public bool Success { get; set; }
	public SkipReason SkipReason { get; set; }
	public string? FileName { get; set; }
	public int RowCount { get; set; }
	public bool IsCancelled => SkipReason == SkipReason.Cancelled;
}

public class ExportExcelService
{
    private readonly RapidZ.Core.Logging.Abstractions.IModuleLogger _logger;
	private readonly ExportObjectValidationService _validationService;
	private readonly ExportDataAccess _dataAccess;
	private readonly ExportExcelFormatSettings _formatSettings;
	private readonly ExportSettings _exportSettings;
	private readonly SharedDatabaseSettings _dbSettings;
	private List<string> _generatedFileNames = new List<string>();
	private Dictionary<string, List<string>> _parameterToFileMap = new Dictionary<string, List<string>>();

	public ExportExcelService(ExportObjectValidationService validationService, ExportDataAccess dataAccess)
	{
		_logger = LoggerFactory.GetExportLogger();
		_validationService = validationService;
		_dataAccess = dataAccess;
		
		// Use static configuration cache methods like TradeDataHub
		_formatSettings = ConfigurationCacheService.GetExcelFormatSettings();
		_exportSettings = ConfigurationCacheService.GetExportSettings();
		_dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
	}
	
	// Public property to access export settings
	public ExportSettings ExportSettings => _exportSettings;
	
	// Get a list of files generated for the current input parameters
	public List<string> GetGeneratedFileNames() => _generatedFileNames;
    
    // Clear the generated files list for the new operation
    public void ClearGeneratedFiles()
    {
        _generatedFileNames = new List<string>();
    }	public ExcelResult CreateReport(int combinationNumber, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null, string? customOutputPath = null)
	{
		var processId = _logger.GenerateProcessId();

		var parameterSet = ExportParameterHelper.CreateExportParameterSet(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
		var parameterDisplay = ExportParameterHelper.FormatParametersForDisplay(parameterSet);
		_logger.LogProcessStart(_exportSettings.Logging.OperationLabel, parameterDisplay, processId);

		var reportTimer = Stopwatch.StartNew();

		// Use the cached export settings (loaded in constructor)
		var exportSettings = _exportSettings;			// Use provided view and stored procedure names if specified, otherwise use defaults
			string effectiveViewName = viewName ?? exportSettings.Operation.ViewName;
			string effectiveStoredProcedureName = storedProcedureName ?? exportSettings.Operation.StoredProcedureName;
		string? partialFilePath = null;

		try
		{
			cancellationToken.ThrowIfCancellationRequested();

			_logger.LogStep("Database", "Executing stored procedure", processId);
			var spTimer = Stopwatch.StartNew();
			var (connection, reader, recordCount) = _dataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, cancellationToken, effectiveViewName, effectiveStoredProcedureName);
			spTimer.Stop();

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				_logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);
				if (recordCount == 0)
				{
					var datasetLogger = LoggerFactory.GetDatasetLogger();
			datasetLogger.LogSkippedDataset(new RapidZ.Core.Logging.Models.SkippedDatasetInfo
			{
				ModuleType = "Export",
				CombinationNumber = combinationNumber,
				RowCount = 0,
				Reason = "NoData",
				Parameters = new RapidZ.Core.Logging.Models.ProcessParameters
				{
					FromMonth = fromMonth,
					ToMonth = toMonth,
					HsCode = hsCode,
					Product = product,
					Iec = iec,
					ExporterOrImporter = exporter,
				Country = country,
				Name = name,
					Port = port
				}
			});
					_logger.LogProcessComplete(_exportSettings.Logging.OperationLabel, reportTimer.Elapsed, "No data - skipped", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.NoData, RowCount = 0 };
				}
				if (recordCount > ExportParameterHelper.MAX_EXCEL_ROWS)
				{
					string skippedFileName = Export_FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
					var datasetLogger = LoggerFactory.GetDatasetLogger();
			datasetLogger.LogSkippedDataset(new RapidZ.Core.Logging.Models.SkippedDatasetInfo
			{
				ModuleType = "Export",
				CombinationNumber = combinationNumber,
				RowCount = recordCount,
				Reason = "RowLimit",
				Parameters = new RapidZ.Core.Logging.Models.ProcessParameters
				{
					FromMonth = fromMonth,
					ToMonth = toMonth,
					HsCode = hsCode,
					Product = product,
					Iec = iec,
					ExporterOrImporter = exporter,
				Country = country,
				Name = name,
					Port = port
				}
			});
					// For skipped files, we don't have a full path yet, so use the original method
				_logger.LogSkipped(skippedFileName, recordCount, "Excel row limit exceeded", processId);
					_logger.LogProcessComplete(_exportSettings.Logging.OperationLabel, reportTimer.Elapsed, "Skipped - too many rows", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.ExcelRowLimit, FileName = skippedFileName, RowCount = (int)recordCount };
				}

				cancellationToken.ThrowIfCancellationRequested();

					string fileName = Export_FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
			_logger.LogExcelFileCreationStart(fileName, processId);
				var excelTimer = Stopwatch.StartNew();
				using var package = new ExcelPackage();
				var worksheetName = exportSettings.Operation.WorksheetName;
				var worksheet = package.Workbook.Worksheets.Add(worksheetName);

				cancellationToken.ThrowIfCancellationRequested();

				int fieldCount = reader.FieldCount;
				
				cancellationToken.ThrowIfCancellationRequested();

				// Load data from reader
				worksheet.Cells["A1"].LoadFromDataReader(reader, true);
				
				cancellationToken.ThrowIfCancellationRequested();
				
				int lastRow = (int)recordCount + 1; // include header
				
				// Apply range-based formatting for better performance
				ApplyOptimizedExcelFormatting(worksheet, lastRow, fieldCount);
				
				cancellationToken.ThrowIfCancellationRequested();

				// Use custom output path if provided, otherwise use default
		string outputDir = customOutputPath ?? exportSettings.Files.OutputDirectory;
		Directory.CreateDirectory(outputDir);
		string outputPath = Path.Combine(outputDir, fileName);
				partialFilePath = outputPath; // Track for cleanup if cancelled

				var saveTimer = Stopwatch.StartNew();
				// Use memory stream for better performance with smaller files
				if (recordCount < 50000) // Use memory stream for smaller datasets
				{
					// More accurate estimation based on field count and data types
					int estimatedSize = Math.Min(50 * 1024 * 1024, // Cap at 50MB
						Math.Max(1024 * 1024, (int)recordCount * fieldCount * 50)); // More realistic per-cell estimate
					using var memoryStream = new MemoryStream(estimatedSize);
					package.SaveAs(memoryStream);
					File.WriteAllBytes(outputPath, memoryStream.ToArray());
				}
				else
				{
					// For larger datasets, use direct file save
					package.SaveAs(new FileInfo(outputPath));
				}
				_logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
				saveTimer.Stop();

				partialFilePath = null; // File successfully created, don't clean up

				_logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
				excelTimer.Stop();
				_logger.LogProcessComplete(_exportSettings.Logging.OperationLabel, reportTimer.Elapsed, $"Success - {outputPath}", processId);
				// Add to the list of generated files
				_generatedFileNames.Add(fileName);
				
				return new ExcelResult { Success = true, SkipReason = SkipReason.None, FileName = fileName, RowCount = (int)recordCount };
			}
			finally
			{
				reader.Dispose();
				connection.Dispose();
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogProcessComplete(_exportSettings.Logging.OperationLabel, reportTimer.Elapsed, "Cancelled by user", processId);
			
			// Clean up partial file if it exists
			CancellationCleanupHelper.SafeDeletePartialFile(partialFilePath, processId);
			
			return new ExcelResult { Success = false, SkipReason = SkipReason.Cancelled, RowCount = 0 };
		}
		catch (Exception ex)
		{
			_logger.LogError($"Process failed: {ex.Message}", ex, processId);
			_logger.LogProcessComplete(_exportSettings.Logging.OperationLabel, reportTimer.Elapsed, "Failed with error", processId);
			reportTimer.Stop();
			throw;
		}
	}

	private void ApplyOptimizedExcelFormatting(ExcelWorksheet worksheet, int lastRow, int colCount)
	{
		if (lastRow <= 1) return;

		try
		{
			// Use the cached format settings (loaded in constructor)
			var formatSettings = _formatSettings;
			
			// Apply font settings to entire worksheet range at once (much faster than cell-by-cell)
			var allCellsRange = worksheet.Cells[1, 1, lastRow, colCount];
			allCellsRange.Style.Font.Name = formatSettings.FontName;
			allCellsRange.Style.Font.Size = formatSettings.FontSize;
			allCellsRange.Style.WrapText = formatSettings.WrapText;

			// Apply column-specific formatting in batches
			foreach (int dateCol in formatSettings.DateColumns)
				if (dateCol > 0 && dateCol <= colCount)
					worksheet.Column(dateCol).Style.Numberformat.Format = formatSettings.DateFormat;
					
			foreach (int textCol in formatSettings.TextColumns)
				if (textCol > 0 && textCol <= colCount)
					worksheet.Column(textCol).Style.Numberformat.Format = "@";

			var borderStyle = (formatSettings.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
				? ExcelBorderStyle.None : ExcelBorderStyle.Thin;

			// Apply header formatting to entire header row at once
			var headerRange = worksheet.Cells[1, 1, 1, colCount];
			headerRange.Style.Font.Bold = true;
			headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
			headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(formatSettings.HeaderBackgroundColor));
			headerRange.Style.Border.Top.Style = borderStyle;
			headerRange.Style.Border.Left.Style = borderStyle;
			headerRange.Style.Border.Right.Style = borderStyle;
			headerRange.Style.Border.Bottom.Style = borderStyle;
			
			// Apply header alignment
			headerRange.Style.HorizontalAlignment = GetHorizontalAlignment(formatSettings.HeaderHorizontalAlignment);
			headerRange.Style.VerticalAlignment = GetVerticalAlignment(formatSettings.HeaderVerticalAlignment);
			
			// Apply freeze pane if enabled
			if (formatSettings.FreezeTopRow)
			{
				worksheet.View.FreezePanes(2, 1);
			}

			// Apply data formatting to entire data range at once
			if (lastRow > 1)
			{
				var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
				dataRange.Style.Border.Top.Style = borderStyle;
				dataRange.Style.Border.Left.Style = borderStyle;
				dataRange.Style.Border.Right.Style = borderStyle;
				dataRange.Style.Border.Bottom.Style = borderStyle;
			}

			// Auto-fit columns using dynamic sample size based on dataset size
			if (formatSettings.AutoFitColumns)
			{
				int sampleRows = lastRow > formatSettings.LargeDatasetThreshold 
					? formatSettings.AutoFitSampleRowsLarge 
					: formatSettings.AutoFitSampleRows;
				int sampleEndRow = Math.Min(lastRow, sampleRows);
				worksheet.Cells[1, 1, sampleEndRow, colCount].AutoFitColumns();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error applying formatting from JSON configuration: {ex.Message}", ex, "FORMAT");
			throw; // Re-throw the exception since we no longer have fallback formatting
		}
	}
	
	private OfficeOpenXml.Style.ExcelHorizontalAlignment GetHorizontalAlignment(string alignment)
	{
		return alignment?.ToLower() switch
		{
			"left" => OfficeOpenXml.Style.ExcelHorizontalAlignment.Left,
			"center" => OfficeOpenXml.Style.ExcelHorizontalAlignment.Center,
			"right" => OfficeOpenXml.Style.ExcelHorizontalAlignment.Right,
			"justify" => OfficeOpenXml.Style.ExcelHorizontalAlignment.Justify,
			_ => OfficeOpenXml.Style.ExcelHorizontalAlignment.Center
		};
	}
	
	private OfficeOpenXml.Style.ExcelVerticalAlignment GetVerticalAlignment(string alignment)
	{
		return alignment?.ToLower() switch
		{
			"top" => OfficeOpenXml.Style.ExcelVerticalAlignment.Top,
			"middle" => OfficeOpenXml.Style.ExcelVerticalAlignment.Center,
			"bottom" => OfficeOpenXml.Style.ExcelVerticalAlignment.Bottom,
			_ => OfficeOpenXml.Style.ExcelVerticalAlignment.Center
		};
	}
}