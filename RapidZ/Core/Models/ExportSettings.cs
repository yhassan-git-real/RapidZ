using System.Collections.Generic;
using RapidZ.Core.Models;

namespace RapidZ.Core.Models
{
    public class ExportSettingsRoot
    {
        public ExportSettings ExportSettings { get; set; } = new ExportSettings();
    }

    public class ExportSettings
    {
        public ExportOperationSettings Operation { get; set; } = new ExportOperationSettings();
        public ExportFileSettings Files { get; set; } = new ExportFileSettings();
        public ExportLoggingSettings Logging { get; set; } = new ExportLoggingSettings();
        public ExportObjectsSettings? ExportObjects { get; set; }
    }

    public class ExportOperationSettings
    {
        public string StoredProcedureName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public string OrderByColumn { get; set; } = string.Empty;
        public string WorksheetName { get; set; } = string.Empty;
    }

    public class ExportFileSettings
    {
        public string OutputDirectory { get; set; } = string.Empty;
    }

    public class ExportLoggingSettings
    {
        public string OperationLabel { get; set; } = string.Empty;
        public string LogFilePrefix { get; set; } = string.Empty;
        public string LogFileExtension { get; set; } = string.Empty;
    }

    public class ExportObjectsSettings
    {
        public string DefaultViewName { get; set; } = string.Empty;
        public string DefaultStoredProcedureName { get; set; } = string.Empty;
        public List<DbObjectOption> Views { get; set; } = new List<DbObjectOption>();
        public List<DbObjectOption> StoredProcedures { get; set; } = new List<DbObjectOption>();
    }
}