using System;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Common.ViewModels;
using RapidZ.Core.Services;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for data filtering logic
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Helper methods for data filtering and preparation
    /// </summary>
    private void UpdateFromMonthValue()
    {
        ExportDataFilter.FromMonth = $"{_fromYear}{_fromMonth:D2}";
    }
    
    private void UpdateToMonthValue()
    {
        ExportDataFilter.ToMonth = $"{_toYear}{_toMonth:D2}";
    }
    
    private void SetCurrentFilterInService()
    {
        if (Services?.UIActionService is UIActionService uiActionService)
        {
            uiActionService.SetCurrentExportFilter(ExportDataFilter);
            uiActionService.SetSelectedView(SelectedView?.Name ?? "");
            uiActionService.SetSelectedStoredProcedure(SelectedStoredProcedure?.Name ?? "");
        }
    }
    
    private void PrepareFilterWithDefaults()
    {
        var defaultWildcard = "*"; // Use simple default
        
        ExportDataFilter.HSCode = string.IsNullOrEmpty(ExportDataFilter.HSCode) 
            ? defaultWildcard 
            : ExportDataFilter.HSCode;
            
        ExportDataFilter.Product = string.IsNullOrEmpty(ExportDataFilter.Product) 
            ? defaultWildcard 
            : ExportDataFilter.Product;
            
        ExportDataFilter.Exporter = string.IsNullOrEmpty(ExportDataFilter.Exporter) 
            ? defaultWildcard 
            : ExportDataFilter.Exporter;
            
        ExportDataFilter.IEC = string.IsNullOrEmpty(ExportDataFilter.IEC) 
            ? defaultWildcard 
            : ExportDataFilter.IEC;
            
        ExportDataFilter.ForeignParty = string.IsNullOrEmpty(ExportDataFilter.ForeignParty) 
            ? defaultWildcard 
            : ExportDataFilter.ForeignParty;
            
        ExportDataFilter.ForeignCountry = string.IsNullOrEmpty(ExportDataFilter.ForeignCountry) 
            ? defaultWildcard 
            : ExportDataFilter.ForeignCountry;
            
        ExportDataFilter.Port = string.IsNullOrEmpty(ExportDataFilter.Port) 
            ? defaultWildcard 
            : ExportDataFilter.Port;
    }
}
