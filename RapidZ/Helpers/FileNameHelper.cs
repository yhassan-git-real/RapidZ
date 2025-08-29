using System;
using RapidZ.Models;

namespace RapidZ.Helpers;

// Helper class for generating filenames for exported files
public static class FileNameHelper
{
    // Generate export filename based on filter parameters
    public static string GenerateExportFileName(ExportDataFilter filter)
    {
        var fileName = "";
        
        // Add HS Code if not wildcard
        if (filter.HSCode != "%" && !string.IsNullOrEmpty(filter.HSCode))
        {
            fileName += filter.HSCode;
        }
        
        // Add Product if not wildcard
        if (filter.Product != "%" && !string.IsNullOrEmpty(filter.Product))
        {
            fileName += "_" + CleanFileName(filter.Product);
        }
        
        // Add IEC if not wildcard
        if (filter.IEC != "%" && !string.IsNullOrEmpty(filter.IEC))
        {
            fileName += "_" + filter.IEC;
        }
        
        // Add Exporter if not wildcard
        if (filter.Exporter != "%" && !string.IsNullOrEmpty(filter.Exporter))
        {
            fileName += "_" + CleanFileName(filter.Exporter);
        }
        
        // Add Foreign Country if not wildcard
        if (filter.ForeignCountry != "%" && !string.IsNullOrEmpty(filter.ForeignCountry))
        {
            fileName += "_" + CleanFileName(filter.ForeignCountry);
        }
        
        // Add Foreign Party if not wildcard
        if (filter.ForeignParty != "%" && !string.IsNullOrEmpty(filter.ForeignParty))
        {
            fileName += "_" + CleanFileName(filter.ForeignParty);
        }
        
        // Add Port if not wildcard
        if (filter.Port != "%" && !string.IsNullOrEmpty(filter.Port))
        {
            fileName += "_" + CleanFileName(filter.Port);
        }
        
        // Remove leading underscore if present
        if (fileName.StartsWith("_"))
        {
            fileName = fileName.Substring(1);
        }
        
        // Add month information
        string monthText = GetMonthRangeText(filter.FromMonth, filter.ToMonth);
        
        return $"{fileName}_{monthText}{filter.Mode.Substring(0, 3).ToUpper()}.xlsx";
    }
    
    // Get month range text (like JAN22-MAR22 or JAN22 if from/to are same)
    private static string GetMonthRangeText(string fromMonth, string toMonth)
    {
        if (fromMonth.Length < 6 || toMonth.Length < 6)
            return string.Empty;
            
        string fromMonthName = GetMonthName(int.Parse(fromMonth.Substring(4, 2)));
        string fromYear = fromMonth.Substring(2, 2);
        string from = fromMonthName + fromYear;
        
        string toMonthName = GetMonthName(int.Parse(toMonth.Substring(4, 2)));
        string toYear = toMonth.Substring(2, 2);
        string to = toMonthName + toYear;
        
        if (from == to)
        {
            return from;
        }
        
        return $"{from}-{to}";
    }
    
    // Get month name from month number
    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "JAN",
            2 => "FEB",
            3 => "MAR",
            4 => "APR",
            5 => "MAY",
            6 => "JUN",
            7 => "JUL",
            8 => "AUG",
            9 => "SEP",
            10 => "OCT",
            11 => "NOV",
            12 => "DEC",
            _ => throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12")
        };
    }
    
    // Clean a string for use in a filename
    private static string CleanFileName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        // Replace spaces with underscores and remove invalid filename characters
        return string.Join("_", input.Split(Path.GetInvalidFileNameChars()));
    }
}
