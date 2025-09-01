using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using RapidZ.Views.Controls;

namespace RapidZ.Core.Services
{
    /// <summary>
    /// Service for showing dialog messages in Avalonia (equivalent to MessageBox in WPF)
    /// </summary>
    public static class DialogService
    {
        private static Window? _parentWindow;
        
        /// <summary>
        /// Sets the parent window for dialogs
        /// </summary>
        public static void SetParentWindow(Window window)
        {
            _parentWindow = window;
        }
        
        
        /// <summary>
        /// Shows a processing complete dialog with detailed information
        /// </summary>
        /// <param name="operationType">Type of operation (Import/Export)</param>
        /// <param name="fileCount">Number of files generated</param>
        /// <param name="parameterCount">Number of parameter combinations checked</param>
        /// <param name="combinationCount">Total combination count</param>
        /// <param name="fileNames">List of generated file names (optional)</param>
        /// <param name="processingTime">Total processing time (optional)</param>
        public static async Task ShowProcessingCompleteAsync(
            string operationType, 
            int fileCount, 
            int parameterCount, 
            int combinationCount,
            List<string>? fileNames = null,
            TimeSpan? processingTime = null)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_parentWindow != null)
                {
                    await ProcessingCompleteDialog.Show(
                        _parentWindow,
                        operationType,
                        fileCount,
                        parameterCount,
                        combinationCount,
                        fileNames,
                        processingTime);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PROCESSING COMPLETE] {operationType}: {fileCount} files, {parameterCount} parameters");
                }
            });
        }
    }
}
