using System;

namespace RapidZ.Helpers;

// Helper class for date-related operations
public static class DateHelper
{
    // Convert YearMonth format (YYYYMM) to DateTime
    public static DateTime YearMonthToDate(string yearMonth)
    {
        if (string.IsNullOrEmpty(yearMonth) || yearMonth.Length != 6)
        {
            throw new ArgumentException("YearMonth must be in YYYYMM format", nameof(yearMonth));
        }
        
        int year = int.Parse(yearMonth.Substring(0, 4));
        int month = int.Parse(yearMonth.Substring(4, 2));
        
        return new DateTime(year, month, 1);
    }
    
    // Get YearMonth string (YYYYMM) from DateTime
    public static string DateToYearMonth(DateTime date)
    {
        return $"{date.Year}{date.Month:D2}";
    }
    
    // Validate YearMonth format
    public static bool IsValidYearMonth(string yearMonth)
    {
        if (string.IsNullOrEmpty(yearMonth) || yearMonth.Length != 6)
        {
            return false;
        }
        
        // Try parsing year and month
        if (!int.TryParse(yearMonth.Substring(0, 4), out int year))
        {
            return false;
        }
        
        if (!int.TryParse(yearMonth.Substring(4, 2), out int month))
        {
            return false;
        }
        
        // Validate month range
        if (month < 1 || month > 12)
        {
            return false;
        }
        
        // Validate reasonable year range (1900-2100)
        if (year < 1900 || year > 2100)
        {
            return false;
        }
        
        return true;
    }
}
