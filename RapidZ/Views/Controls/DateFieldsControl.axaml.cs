using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System;
using System.Threading;

namespace RapidZ.Views.Controls
{
    public partial class DateFieldsControl : UserControl
    {
        public DateFieldsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void RunQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                if (mainWindow?.Services?.UIActionService != null)
                {
                    await mainWindow.Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RunQuery_Click: {ex.Message}");
            }
        }

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                if (mainWindow?.Services?.UIActionService != null)
                {
                    await mainWindow.Services.UIActionService.HandleGenerateAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExportToExcel_Click: {ex.Message}");
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = this.FindAncestorOfType<MainWindow>();
                mainWindow?.Services?.UIActionService?.HandleReset();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Clear_Click: {ex.Message}");
            }
        }
    }
}
