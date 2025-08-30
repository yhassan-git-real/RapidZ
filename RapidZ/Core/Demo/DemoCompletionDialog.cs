using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using RapidZ.Core.Services;
using RapidZ.Views.Controls;

namespace RapidZ.Core.Demo
{
    /// <summary>
    /// Demo class to show improved completion dialogs
    /// </summary>
    public static class DemoCompletionDialog
    {
        public static async Task ShowDemoExportCompletionAsync()
        {
            // Create sample statistics for demo
            var detailedMessage = @"Export completed successfully!

Files Generated: 5
Records Processed: 1,250
Combinations Processed: 12
Skipped Combinations: 7 total
  • No Data Found: 5 combinations had zero matching records
  • Excel Row Limit: 2 combinations exceeded Excel's 1,048,576 row limit

Operation completed with 41.7% success rate.";

            // Show a demo completion dialog with detailed statistics
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(
                    null, // we don't have a parent window available here
                    "Export Complete",
                    detailedMessage,
                    "",
                    MessageBox.MessageBoxType.Success);
            });
        }
    }
}
