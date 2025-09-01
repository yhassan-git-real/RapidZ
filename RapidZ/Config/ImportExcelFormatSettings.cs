namespace RapidZ.Config
{
    /// <summary>
    /// Excel formatting settings for Import operations
    /// Inherits from base ExcelFormatSettings
    /// </summary>
    public class ImportExcelFormatSettings : ExcelFormatSettings
    {
        public ImportExcelFormatSettings()
        {
            // Default settings specific to import
            DateColumns = new System.Collections.Generic.List<int> { 2 };
            TextColumns = new System.Collections.Generic.List<int> { 1, 3, 4 };
        }
    }
}