using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RapidZ.Views.Controls
{
    public partial class InputFieldsControl : UserControl
    {
        public InputFieldsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
