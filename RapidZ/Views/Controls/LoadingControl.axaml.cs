using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;

namespace RapidZ.Views.Controls;

public partial class LoadingControl : UserControl
{
    public LoadingControl()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mainWindow = this.FindAncestorOfType<MainWindow>();
            mainWindow?.Services?.UIActionService?.HandleCancel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in Cancel_Click: {ex.Message}");
        }
    }
}
