using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;

namespace RapidZ.Views.Controls
{
    public partial class MessageBox : Window
    {
        public string Message { get; private set; } = string.Empty;
        public string Details { get; private set; } = string.Empty;
        public bool HasDetails => !string.IsNullOrEmpty(Details);
        public MaterialIconKind IconKind { get; private set; } = MaterialIconKind.Information;
        public IBrush IconBackground { get; private set; } = new SolidColorBrush(Colors.Blue);

        public MessageBox()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public enum MessageBoxType
        {
            Information,
            Success,
            Warning,
            Error
        }

        public static Task<MessageBox> Show(
            Window parent,
            string title,
            string message,
            string details = "",
            MessageBoxType type = MessageBoxType.Information)
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Message = message,
                Details = details,
            };

            // Set the icon and color based on message type
            switch (type)
            {
                case MessageBoxType.Information:
                    messageBox.IconKind = MaterialIconKind.Information;
                    messageBox.IconBackground = new SolidColorBrush(Color.Parse("#3498db"));
                    break;
                case MessageBoxType.Success:
                    messageBox.IconKind = MaterialIconKind.CheckCircle;
                    messageBox.IconBackground = new SolidColorBrush(Color.Parse("#2ecc71"));
                    break;
                case MessageBoxType.Warning:
                    messageBox.IconKind = MaterialIconKind.Alert;
                    messageBox.IconBackground = new SolidColorBrush(Color.Parse("#f39c12"));
                    break;
                case MessageBoxType.Error:
                    messageBox.IconKind = MaterialIconKind.Close;
                    messageBox.IconBackground = new SolidColorBrush(Color.Parse("#e74c3c"));
                    break;
            }

            // Show dialog as modal
            if (parent != null)
            {
                messageBox.ShowDialog(parent);
            }
            else
            {
                messageBox.Show();
            }

            return Task.FromResult(messageBox);
        }
    }
}
