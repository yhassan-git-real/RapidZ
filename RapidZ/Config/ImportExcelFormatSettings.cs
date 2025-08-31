using System.Linq;

namespace RapidZ.Config
{
    /// <summary>
    /// Excel formatting settings for Import operations
    /// Inherits from base ExcelFormatSettings
    /// </summary>
    public class ImportExcelFormatSettings : ExcelFormatSettings
    {
        // Property for backward compatibility with JSON deserialization
        public int[]? DateColumnsArray
        {
            get => DateColumns?.ToArray();
            set
            {
                if (value != null)
                {
                    DateColumns = new System.Collections.Generic.List<int>(value);
                }
            }
        }

        // Property for backward compatibility with JSON deserialization
        public int[]? TextColumnsArray
        {
            get => TextColumns?.ToArray();
            set
            {
                if (value != null)
                {
                    TextColumns = new System.Collections.Generic.List<int>(value);
                }
            }
        }
        
        public ImportExcelFormatSettings()
        {
            // Default settings specific to import
            DateColumns = new System.Collections.Generic.List<int> { 2 };
            TextColumns = new System.Collections.Generic.List<int> { 1, 3, 4 };
        }
    }
}