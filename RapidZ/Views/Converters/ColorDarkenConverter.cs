using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RapidZ.Views.Converters
{
    public class ColorDarkenConverter : IValueConverter
    {
        public static readonly ColorDarkenConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Color color)
                return Colors.Gray;

            // Darken the color by reducing RGB values by 20%
            var factor = 0.8;
            var darkenedColor = Color.FromArgb(
                color.A,
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor)
            );

            return darkenedColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}