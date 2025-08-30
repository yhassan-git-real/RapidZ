using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RapidZ.Views.Converters
{
    /// <summary>
    /// Converter that changes brush color based on validation state
    /// </summary>
    public class BoolToValidationBrushConverter : IValueConverter
    {
        // Default colors
        private readonly IBrush _validBrush = new SolidColorBrush(Color.Parse("#D0D8E8")); // Default border color
        private readonly IBrush _invalidBrush = new SolidColorBrush(Color.Parse("#E53935")); // Red for invalid

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isValid)
            {
                // Return red brush for invalid state, default brush for valid state
                return isValid ? _validBrush : _invalidBrush;
            }

            // Default to valid state
            return _validBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
