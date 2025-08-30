using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RapidZ.Views.Controls;

public partial class StatusPanelControl : UserControl
{
    public StatusPanelControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
