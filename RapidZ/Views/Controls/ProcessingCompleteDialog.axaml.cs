using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;

namespace RapidZ.Views.Controls
{
    public partial class ProcessingCompleteDialog : Window, INotifyPropertyChanged
    {
        private string _filesGeneratedText = string.Empty;
        private string _totalProcessedText = string.Empty;
        private string _statusText = string.Empty;
        private string _processingTimeText = string.Empty;
        private List<string> _fileNames = new List<string>();
        private MaterialIconKind _iconKind = MaterialIconKind.CheckCircle;
        private IBrush _iconBackground = new SolidColorBrush(Color.Parse("#2ecc71"));
        
        public string FilesGeneratedText 
        { 
            get => _filesGeneratedText;
            private set 
            {
                if (_filesGeneratedText != value) 
                {
                    _filesGeneratedText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FilesGeneratedTextValue));
                }
            } 
        }
        
        public string FilesGeneratedTextValue => ExtractValue(FilesGeneratedText);
        
        public string TotalProcessedText 
        { 
            get => _totalProcessedText;
            private set 
            {
                if (_totalProcessedText != value) 
                {
                    _totalProcessedText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalProcessedTextValue));
                }
            } 
        }
        
        public string TotalProcessedTextValue => ExtractValue(TotalProcessedText);
        
        public string StatusText 
        { 
            get => _statusText;
            private set 
            {
                if (_statusText != value) 
                {
                    _statusText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusTextValue));
                }
            } 
        }
        
        public string StatusTextValue => ExtractValue(StatusText);
        
        public string ProcessingTimeText 
        { 
            get => _processingTimeText;
            private set 
            {
                if (_processingTimeText != value) 
                {
                    _processingTimeText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProcessingTimeTextValue));
                }
            } 
        }
        
        public string ProcessingTimeTextValue => ExtractValue(ProcessingTimeText);
        
        public List<string> FileNames 
        { 
            get => _fileNames;
            private set 
            {
                _fileNames = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasFileDetails));
            } 
        }
        
        public bool HasFileDetails => FileNames.Count > 0;
        public bool HasPerformanceData => !string.IsNullOrEmpty(ProcessingTimeText);
        
        public MaterialIconKind IconKind 
        { 
            get => _iconKind;
            private set 
            {
                if (_iconKind != value) 
                {
                    _iconKind = value;
                    OnPropertyChanged();
                }
            } 
        }
        
        public IBrush IconBackground 
        { 
            get => _iconBackground;
            private set 
            {
                if (_iconBackground != value) 
                {
                    _iconBackground = value;
                    OnPropertyChanged();
                }
            } 
        }
        
        public new event PropertyChangedEventHandler? PropertyChanged;

        public ProcessingCompleteDialog()
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
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private string ExtractValue(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            int colonIndex = text.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < text.Length - 1)
            {
                return text.Substring(colonIndex + 1).Trim();
            }
            
            return text;
        }

        public static Task<ProcessingCompleteDialog> Show(
            Window parent,
            string operationType,
            int fileCount,
            int parameterCount,
            int combinationCount,
            List<string> fileNames = null,
            TimeSpan? processingTime = null)
        {
            var dialog = new ProcessingCompleteDialog
            {
                Title = $"{operationType} Processing Complete",
                FilesGeneratedText = $"Files Generated: {fileCount} Excel file{(fileCount != 1 ? "s" : "")} created successfully",
                TotalProcessedText = $"Total Processed: {parameterCount} parameter combinations checked",
                StatusText = $"Operation completed successfully! {fileCount} file{(fileCount != 1 ? "s" : "")} generated.",
                FileNames = fileNames ?? new List<string>(),
                ProcessingTimeText = processingTime.HasValue ? $"Total Process: Completed in {processingTime.Value:mm\\:ss\\.fff}" : string.Empty,
                IconKind = MaterialIconKind.CheckCircle,
                IconBackground = new SolidColorBrush(Color.Parse("#2ecc71"))
            };
            


            // Show dialog as modal
            if (parent != null)
            {
                dialog.ShowDialog(parent);
            }
            else
            {
                dialog.Show();
            }

            return Task.FromResult(dialog);
        }
    }
}
