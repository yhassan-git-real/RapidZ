using System.Collections.Generic;

namespace RapidZ.Config
{
    public class ExportExcelFormatSettings
    {
        public string FontName { get; set; } = "Times New Roman";
        public int FontSize { get; set; } = 10;
        public string HeaderBackgroundColor { get; set; } = "#4F81BD";
        public string BorderStyle { get; set; } = "Thin";
        public int AutoFitSampleRows { get; set; } = 100;
        public string DateFormat { get; set; } = "dd-mmm-yy";
        public List<int> DateColumns { get; set; } = new List<int> { 3 };
        public List<int> TextColumns { get; set; } = new List<int> { 1, 2, 4 };
        public bool WrapText { get; set; } = false;
        public bool AutoFitColumns { get; set; } = true;
    }
}