using System;

namespace RapidZ.Models;

// Export data record from database query results
public class ExportData
{
    // Database mapped properties
    public string? HSCode { get; set; }
    public string? Product { get; set; }
    public string? Exporter { get; set; }
    public string? IEC { get; set; }
    public string? ForeignParty { get; set; }
    public string? ForeignCountry { get; set; }
    public string? Port { get; set; }
    public DateTime? SBDate { get; set; }
    public string? Mode { get; set; }
    
    // Add additional properties as needed based on database schema
}
