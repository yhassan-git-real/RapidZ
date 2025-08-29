namespace RapidZ.Models;

// Filter criteria for export data queries
public class ExportDataFilter
{
    // HS code for product classification
    public string HSCode { get; set; } = string.Empty;
    
    // Product description
    public string Product { get; set; } = string.Empty;
    
    // Exporter name
    public string Exporter { get; set; } = string.Empty;
    
    // Importer-Exporter Code
    public string IEC { get; set; } = string.Empty;
    
    // Foreign party name
    public string ForeignParty { get; set; } = string.Empty;
    
    // Foreign country
    public string ForeignCountry { get; set; } = string.Empty;
    
    // Port name/code
    public string Port { get; set; } = string.Empty;
    
    // Start period (YYYYMM format)
    public string FromMonth { get; set; } = string.Empty;
    
    // End period (YYYYMM format)
    public string ToMonth { get; set; } = string.Empty;
    
    // Import or Export mode
    public string Mode { get; set; } = "Export";
}
