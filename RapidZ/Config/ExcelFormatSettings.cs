using System.Collections.Generic;

namespace RapidZ.Config
{
    /// <summary>
    /// Base class for Excel formatting settings, used by both Import and Export
    /// </summary>
    public class ExcelFormatSettings
    {
        public string FontName { get; set; } = "Times New Roman";
        public int FontSize { get; set; } = 10;
        public string HeaderBackgroundColor { get; set; } = "#4F81BD";
        public string BorderStyle { get; set; } = "Thin";
        public string DateFormat { get; set; } = "dd-mmm-yy";
        public int AutoFitSampleRows { get; set; } = 100;
        public int AutoFitSampleRowsLarge { get; set; } = 50;
        public int LargeDatasetThreshold { get; set; } = 100000;
        public bool WrapText { get; set; } = false;
        public bool AutoFitColumns { get; set; } = true;
        
        // Using List<int> for better flexibility with serialization
        public List<int> DateColumns { get; set; } = new List<int>();
        public List<int> TextColumns { get; set; } = new List<int>();
    }
}
