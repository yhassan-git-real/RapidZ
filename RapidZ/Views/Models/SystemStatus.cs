using Avalonia.Media;

namespace RapidZ.Views.Models;

public enum SystemStatus
{
    Idle,
    Processing,
    Completed,
    Failed
}

public static class SystemStatusExtensions
{
    public static string GetStatusMessage(this SystemStatus status)
    {
        return status switch
        {
            SystemStatus.Idle => "System Ready",
            SystemStatus.Processing => "Processing... Please wait",
            SystemStatus.Completed => "Process Completed Successfully",
            SystemStatus.Failed => "Execution Failed, Check Logs",
            _ => "Unknown Status"
        };
    }
    
    public static IBrush GetStatusColor(this SystemStatus status)
    {
        return status switch
        {
            SystemStatus.Idle => new SolidColorBrush(Color.FromRgb(153, 153, 153)),      // Grey
            SystemStatus.Processing => new SolidColorBrush(Color.FromRgb(30, 136, 229)),  // Blue
            SystemStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
            SystemStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // Red
            _ => new SolidColorBrush(Color.FromRgb(153, 153, 153))                       // Default Grey
        };
    }
}
