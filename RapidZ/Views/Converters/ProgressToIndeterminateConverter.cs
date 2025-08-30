using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RapidZ.Views.Converters;

public class ProgressToIndeterminateConverter : IValueConverter
{
    public static readonly ProgressToIndeterminateConverter Instance = new();
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            bool isIndeterminate = progress <= 0;
            
            // Check if we need to invert the result (for visibility of percentage text)
            if (parameter?.ToString() == "invert")
            {
                return !isIndeterminate;
            }
            
            return isIndeterminate;
        }
        
        // Default behavior based on parameter
        if (parameter?.ToString() == "invert")
        {
            return false; // Don't show percentage text if no valid progress
        }
        
        return true; // Show indeterminate progress bar
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}