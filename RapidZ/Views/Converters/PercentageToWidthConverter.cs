using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RapidZ.Views.Converters
{
    public class PercentageToWidthConverter : IValueConverter
    {
        public static readonly PercentageToWidthConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double percentage || parameter is not string maxWidthStr)
                return 0.0;

            if (!double.TryParse(maxWidthStr, out var maxWidth))
                return 0.0;

            // Convert percentage (0-100) to actual width
            var width = (percentage / 100.0) * maxWidth;
            return Math.Max(0, Math.Min(width, maxWidth));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}