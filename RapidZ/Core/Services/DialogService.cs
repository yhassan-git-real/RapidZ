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
        /// Shows a warning dialog message
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <param name="details">Optional details to display</param>
        public static async Task ShowWarningAsync(string message, string title = "Warning", string details = "")
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_parentWindow != null)
                {
                    await MessageBox.Show(
                        _parentWindow, 
                        title, 
                        message, 
                        details,
                        MessageBox.MessageBoxType.Warning);
                }
                else
                {
                    Console.WriteLine($"[WARNING DIALOG] {title}: {message}");
                    System.Diagnostics.Debug.WriteLine($"[WARNING DIALOG] {title}: {message}");
                }
            });
        }

        /// <summary>
        /// Shows an error dialog message
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <param name="details">Optional details to display</param>
        public static async Task ShowErrorAsync(string message, string title = "Error", string details = "")
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_parentWindow != null)
                {
                    await MessageBox.Show(
                        _parentWindow, 
                        title, 
                        message, 
                        details,
                        MessageBox.MessageBoxType.Error);
                }
                else
                {
                    Console.WriteLine($"[ERROR DIALOG] {title}: {message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR DIALOG] {title}: {message}");
                }
            });
        }

        /// <summary>
        /// Shows an information dialog message
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <param name="details">Optional details to display</param>
        public static async Task ShowInfoAsync(string message, string title = "Information", string details = "")
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_parentWindow != null)
                {
                    await MessageBox.Show(
                        _parentWindow, 
                        title, 
                        message, 
                        details,
                        MessageBox.MessageBoxType.Information);
                }
                else
                {
                    Console.WriteLine($"[INFO DIALOG] {title}: {message}");
                    System.Diagnostics.Debug.WriteLine($"[INFO DIALOG] {title}: {message}");
                }
            });
        }
        
        /// <summary>
        /// Shows a success dialog message
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <param name="details">Optional details to display</param>
        public static async Task ShowSuccessAsync(string message, string title = "Success", string details = "")
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_parentWindow != null)
                {
                    await MessageBox.Show(
                        _parentWindow, 
                        title, 
                        message, 
                        details,
                        MessageBox.MessageBoxType.Success);
                }
                else
                {
                    Console.WriteLine($"[SUCCESS DIALOG] {title}: {message}");
                    System.Diagnostics.Debug.WriteLine($"[SUCCESS DIALOG] {title}: {message}");
                }
            });
        }
    }
}
