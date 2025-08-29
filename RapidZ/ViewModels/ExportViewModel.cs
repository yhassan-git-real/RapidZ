using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RapidZ.Models;
using RapidZ.Services;
using RapidZ.Helpers;

namespace RapidZ.ViewModels;

public class ExportViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly ExcelService _excelService;
    private readonly ConfigurationService _configService;
    
    public ExportViewModel(
        DatabaseService databaseService, 
        ExcelService excelService, 
        ConfigurationService configService)
    {
        _databaseService = databaseService;
        _excelService = excelService;
        _configService = configService;
    }
    
    // Process export data based on filter parameters
    public async Task<bool> ProcessExportAsync(ExportDataFilter filter)
    {
        try
        {
            // Get filter parameters and process multiple values
            var hsCodeValues = filter.HSCode.Split(',').Select(x => x.Trim()).ToArray();
            var productValues = filter.Product.Split(',').Select(x => x.Trim()).ToArray();
            var exporterValues = filter.Exporter.Split(',').Select(x => x.Trim()).ToArray();
            var iecValues = filter.IEC.Split(',').Select(x => x.Trim()).ToArray();
            var foreignPartyValues = filter.ForeignParty.Split(',').Select(x => x.Trim()).ToArray();
            var foreignCountryValues = filter.ForeignCountry.Split(',').Select(x => x.Trim()).ToArray();
            var portValues = filter.Port.Split(',').Select(x => x.Trim()).ToArray();
            
            // Track export operations
            var exportTasks = new List<Task<bool>>();
            
            // Process all parameter combinations
            foreach (var hsCode in hsCodeValues)
            {
                foreach (var product in productValues)
                {
                    foreach (var exporter in exporterValues)
                    {
                        foreach (var iec in iecValues)
                        {
                            foreach (var foreignParty in foreignPartyValues)
                            {
                                foreach (var foreignCountry in foreignCountryValues)
                                {
                                    foreach (var port in portValues)
                                    {
                                        // Create a single parameter set
                                        var singleFilter = new ExportDataFilter
                                        {
                                            HSCode = hsCode,
                                            Product = product,
                                            Exporter = exporter,
                                            IEC = iec,
                                            ForeignParty = foreignParty,
                                            ForeignCountry = foreignCountry,
                                            Port = port,
                                            FromMonth = filter.FromMonth,
                                            ToMonth = filter.ToMonth,
                                            Mode = filter.Mode
                                        };
                                        
                                        // Export data for this parameter set
                                        exportTasks.Add(ExportSingleDataSetAsync(singleFilter));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Wait for all export operations to complete
            await Task.WhenAll(exportTasks);
            
            // Return true if all exports succeeded
            return exportTasks.All(t => t.Result);
        }
        catch (Exception)
        {
            // Return false on any exception
            return false;
        }
    }
    
    // Export a single dataset with specific parameter values
    private async Task<bool> ExportSingleDataSetAsync(ExportDataFilter filter)
    {
        try
        {
            // Get stored procedure name from config
            var spName = _configService.AppSettings.StoredProcedures.ExportData;
            
            // Execute stored procedure and get data
            var data = await _databaseService.ExecuteExportQueryAsync(spName, filter);
            
            // Generate filename based on filter parameters
            var fileName = FileNameHelper.GenerateExportFileName(filter);
            var outputPath = System.IO.Path.Combine(
                _configService.AppSettings.Paths.ExcelOutput, 
                fileName);
                
            // Export data to Excel
            await _excelService.ExportToExcelAsync(data, outputPath);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
