using System;
using System.Globalization;

namespace RapidZ.Core.Helpers;

/// <summary>
/// Centralized helper class for date-related operations
/// </summary>
public static class DateHelper
{
    // Constants for date validation
    public const string DEFAULT_DATE_FORMAT = "yyyyMM";
    public const int MIN_DATE_VALUE = 190001;
    public const int MAX_DATE_VALUE = 299912;
    
    private static readonly string[] MonthAbbreviations = 
    {
        "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
        "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
    };
    
    /// <summary>
    /// Convert YearMonth format (YYYYMM) to DateTime
    /// </summary>
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
    
    /// <summary>
    /// Get YearMonth string (YYYYMM) from DateTime
    /// </summary>
    public static string DateToYearMonth(DateTime date)
    {
        return $"{date.Year}{date.Month:D2}";
    }
    
    /// <summary>
    /// Validate YearMonth format (maintains backward compatibility with existing code)
    /// </summary>
    public static bool IsValidYearMonth(string yearMonth)
    {
        return IsValidDateFormat(yearMonth);
    }
    
    /// <summary>
    /// Validate YYYYMM format date string against defined constraints
    /// </summary>
    public static bool IsValidDateFormat(string dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length != 6)
        {
            return false;
        }
        
        if (!int.TryParse(dateString, out int date))
        {
            return false;
        }
        
        int year = date / 100;
        int month = date % 100;
        
        if (month < 1 || month > 12)
        {
            return false;
        }
        
        return date >= MIN_DATE_VALUE && date <= MAX_DATE_VALUE;
    }
    
    /// <summary>
    /// Validate that from date is before or equal to to date
    /// </summary>
    public static bool IsValidDateRange(string fromMonth, string toMonth)
    {
        return IsValidDateFormat(fromMonth) && IsValidDateFormat(toMonth) && 
               int.Parse(fromMonth) <= int.Parse(toMonth);
    }
    
    /// <summary>
    /// Get month name from month number (1-12)
    /// </summary>
    public static string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");
            
        return MonthAbbreviations[month - 1];
    }
    
    /// <summary>
    /// Converts YYYYMM format to MMMYY format (e.g., "202501" -> "JAN25")
    /// </summary>
    public static string ConvertToMonthAbbreviation(string yyyymm, string defaultValue = "UNK00")
    {
        if (string.IsNullOrWhiteSpace(yyyymm) || yyyymm.Length != 6 || !int.TryParse(yyyymm, out _))
        {
            return defaultValue;
        }

        if (!int.TryParse(yyyymm.Substring(0, 4), out int year) || 
            !int.TryParse(yyyymm.Substring(4, 2), out int month))
        {
            return defaultValue;
        }

        if (month < 1 || month > 12)
        {
            return defaultValue == "UNK00" ? "UNK00" : "MMM";
        }

        string monthAbbr = MonthAbbreviations[month - 1];
        string yearSuffix = (year % 100).ToString("D2");
        
        return $"{monthAbbr}{yearSuffix}";
    }
    
    /// <summary>
    /// Builds a month range segment for file names (e.g., "JAN25" or "JAN25-MAR25").
    /// </summary>
    public static string BuildMonthRangeSegment(string fromMonth, string toMonth, string defaultValue = "UNK00")
    {
        string mon1 = ConvertToMonthAbbreviation(fromMonth, defaultValue);
        string mon2 = ConvertToMonthAbbreviation(toMonth, defaultValue);
        
        return (mon1 == mon2) ? mon1 : $"{mon1}-{mon2}";
    }
    
    /// <summary>
    /// Formats month string for display (YYYYMM -> MMM YYYY)
    /// </summary>
    public static string FormatMonthForDisplay(string monthString)
    {
        if (!IsValidDateFormat(monthString))
            return monthString;

        if (DateTime.TryParseExact(monthString + "01", "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        }

        return monthString;
    }
}
