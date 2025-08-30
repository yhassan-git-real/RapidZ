using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using Material.Icons;
using ReactiveUI;
using System.Windows.Input;
using System.Reactive;
using RapidZ.Views.Models;

namespace RapidZ.Views.Controls
{
    public partial class StatusPanel : UserControl
    {
        // Status properties
        public static readonly StyledProperty<string> StatusTitleProperty =
            AvaloniaProperty.Register<StatusPanel, string>(nameof(StatusTitle), "Ready");
        
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<StatusPanel, string>(nameof(Message), string.Empty);
        
        public static readonly StyledProperty<string> DetailsProperty =
            AvaloniaProperty.Register<StatusPanel, string>(nameof(Details), string.Empty);
        
        public static readonly StyledProperty<MaterialIconKind> StatusIconProperty =
            AvaloniaProperty.Register<StatusPanel, MaterialIconKind>(nameof(StatusIcon), MaterialIconKind.Information);
        
        public static readonly StyledProperty<IBrush> StatusColorProperty =
            AvaloniaProperty.Register<StatusPanel, IBrush>(nameof(StatusColor), Brushes.White);
        
        public static readonly StyledProperty<StatusType> StatusTypeProperty =
            AvaloniaProperty.Register<StatusPanel, StatusType>(nameof(StatusType), StatusType.Information);
        
        public static readonly StyledProperty<bool> StatusHasActionProperty =
            AvaloniaProperty.Register<StatusPanel, bool>(nameof(StatusHasAction), false);
        
        public static readonly StyledProperty<string> ActionTextProperty =
            AvaloniaProperty.Register<StatusPanel, string>(nameof(ActionText), "Details");
        
        public static readonly StyledProperty<ICommand> StatusActionCommandProperty =
            AvaloniaProperty.Register<StatusPanel, ICommand>(nameof(StatusActionCommand));
        
        public string StatusTitle
        {
            get => GetValue(StatusTitleProperty);
            set => SetValue(StatusTitleProperty, value);
        }
        
        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        
        public string Details
        {
            get => GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
        }
        
        public MaterialIconKind StatusIcon
        {
            get => GetValue(StatusIconProperty);
            set => SetValue(StatusIconProperty, value);
        }
        
        public IBrush StatusColor
        {
            get => GetValue(StatusColorProperty);
            set => SetValue(StatusColorProperty, value);
        }
        
        public StatusType StatusType
        {
            get => GetValue(StatusTypeProperty);
            set
            {
                SetValue(StatusTypeProperty, value);
                UpdateVisualsByType(value);
            }
        }
        
        public bool StatusHasAction
        {
            get => GetValue(StatusHasActionProperty);
            set => SetValue(StatusHasActionProperty, value);
        }
        
        public string ActionText
        {
            get => GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }
        
        public ICommand StatusActionCommand
        {
            get => GetValue(StatusActionCommandProperty);
            set => SetValue(StatusActionCommandProperty, value);
        }
        
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

        public StatusPanel()
        {
            InitializeComponent();
            DataContext = this;

            // Update visuals when status type changes
            this.GetObservable(StatusTypeProperty).Subscribe(type => UpdateVisualsByType(type));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Helper method to update visuals based on status type
        private void UpdateVisualsByType(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Success:
                    StatusIcon = MaterialIconKind.CheckCircle;
                    StatusColor = new SolidColorBrush(Color.Parse("#2ecc71"));
                    break;
                case StatusType.Warning:
                    StatusIcon = MaterialIconKind.Alert;
                    StatusColor = new SolidColorBrush(Color.Parse("#f39c12"));
                    break;
                case StatusType.Error:
                    StatusIcon = MaterialIconKind.Close;
                    StatusColor = new SolidColorBrush(Color.Parse("#e74c3c"));
                    break;
                case StatusType.Processing:
                    StatusIcon = MaterialIconKind.ProgressClock;
                    StatusColor = new SolidColorBrush(Color.Parse("#3498db"));
                    break;
                case StatusType.Information:
                default:
                    StatusIcon = MaterialIconKind.Information;
                    StatusColor = new SolidColorBrush(Color.Parse("#3498db"));
                    break;
            }
        }
    }
}