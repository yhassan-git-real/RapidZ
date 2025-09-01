using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;
using RapidZ.Views.Models;

namespace RapidZ.Views.Converters
{
    public class StatusToIconConverter : IMultiValueConverter
    {
        public static readonly StatusToIconConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || values[0] is not SystemStatus status)
                return MaterialIconKind.Circle;

            var isProcessing = values[1] is bool processing && processing;

            return status switch
            {
                SystemStatus.Idle => MaterialIconKind.CheckCircle,
                SystemStatus.Processing when isProcessing => MaterialIconKind.Sync,
                SystemStatus.Processing => MaterialIconKind.PlayCircle,
                SystemStatus.Completed => MaterialIconKind.CheckCircle,
                SystemStatus.Failed => MaterialIconKind.AlertCircle,
                _ => MaterialIconKind.Circle
            };
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}