namespace RapidZ.Config
{
    /// <summary>
    /// Excel formatting settings for Export operations
    /// Inherits from base ExcelFormatSettings
    /// </summary>
    public class ExportExcelFormatSettings : ExcelFormatSettings
    {
        public ExportExcelFormatSettings()
        {
            // Default settings specific to export
            DateColumns = new System.Collections.Generic.List<int> { 3 };
            TextColumns = new System.Collections.Generic.List<int> { 1, 2, 4 };
        }
    }
}