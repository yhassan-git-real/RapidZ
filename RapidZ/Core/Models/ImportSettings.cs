using System.Collections.Generic;
using RapidZ.Core.Models;

namespace RapidZ.Core.Models
{
    public class ImportSettingsRoot
    {
        public ImportSettings ImportSettings { get; set; } = new ImportSettings();
    }

    public class ImportSettings
    {
        public ImportDatabaseSettings Database { get; set; } = new ImportDatabaseSettings();
        public ImportFileSettings Files { get; set; } = new ImportFileSettings();
        public ImportLoggingSettings Logging { get; set; } = new ImportLoggingSettings();
        public ImportObjectsSettings? ImportObjects { get; set; }
    }

    public class ImportDatabaseSettings
    {
        public string StoredProcedureName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public string OrderByColumn { get; set; } = string.Empty;
        public string WorksheetName { get; set; } = string.Empty;
    }

    public class ImportFileSettings
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public string FileSuffix { get; set; } = string.Empty;
    }

    public class ImportLoggingSettings
    {
        public string OperationLabel { get; set; } = string.Empty;
        public string LogFilePrefix { get; set; } = string.Empty;
        public string LogFileExtension { get; set; } = string.Empty;
    }

    public class ImportObjectsSettings
    {
        public string DefaultViewName { get; set; } = string.Empty;
        public string DefaultStoredProcedureName { get; set; } = string.Empty;
        public List<DbObjectOption> Views { get; set; } = new List<DbObjectOption>();
        public List<DbObjectOption> StoredProcedures { get; set; } = new List<DbObjectOption>();
    }
}