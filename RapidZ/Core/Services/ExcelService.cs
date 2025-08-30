using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RapidZ.Core.Services;

namespace RapidZ.Core.Services;

// Service to handle Excel export operations
public class ExcelService
{
    private readonly ILogger<ExcelService> _logger;
    private readonly ConfigurationService _configService;

    public ExcelService(ILogger<ExcelService> logger, ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
        
        // Set the EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    
    // Export data from SqlDataReader to Excel file
    public async Task ExportToExcelAsync(SqlDataReader reader, string outputPath)
    {
        try
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("ExportData");
            
            // Get formatting settings from configuration
            var formatting = _configService.AppSettings.ExcelFormatting;
            
            // Load entire dataset at once instead of row by row for performance
            worksheet.Cells["A2"].LoadFromDataReader(reader, true);
            
            // If there's no data, just return
            if (worksheet.Dimension == null)
            {
                _logger.LogInformation("No data to export");
                return;
            }
            
            int rowCount = worksheet.Dimension.End.Row;
            int colCount = worksheet.Dimension.End.Column;
            
            // Apply formatting in bulk operations
            if (rowCount > 1)
            {
                var dataRange = worksheet.Cells[2, 1, rowCount, colCount];
                
                // Apply styling in bulk
                dataRange.Style.Font.Name = formatting.FontName;
                dataRange.Style.Font.Size = formatting.FontSize;
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                
                // Format date columns (assuming column 3 is date based on original VB code)
                var dateRange = worksheet.Cells[2, 3, rowCount, 3];
                dateRange.Style.Numberformat.Format = formatting.DateFormat;
                
                // Auto-fit columns
                worksheet.Cells[1, 1, rowCount, colCount].AutoFitColumns();
            }
            
            // Save the Excel file
            await package.SaveAsAsync(new FileInfo(outputPath));
            
            _logger.LogInformation($"Excel export completed: {outputPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error exporting data to Excel: {outputPath}");
            throw;
        }
    }
}
