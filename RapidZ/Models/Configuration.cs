namespace RapidZ.Models;

public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public StoredProcedureSettings StoredProcedures { get; set; } = new();
    public ViewSettings Views { get; set; } = new();
    public PathSettings Paths { get; set; } = new();
    public ApplicationSettings ApplicationSettings { get; set; } = new();
    public ExcelFormattingSettings ExcelFormatting { get; set; } = new();
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
}

public class StoredProcedureSettings
{
    public string ExportData { get; set; } = string.Empty;
}

public class ViewSettings
{
    public string ExportDataView { get; set; } = string.Empty;
}

public class PathSettings
{
    public string ExcelOutput { get; set; } = string.Empty;
    public string LogFiles { get; set; } = string.Empty;
}

public class ApplicationSettings
{
    public string DefaultWildcard { get; set; } = "%";
    public string DateFormat { get; set; } = string.Empty;
    public string DefaultDateColumn { get; set; } = "sb_DATE";
    public string OrderByClause { get; set; } = "ORDER BY [sb_DATE]";
}

public class ExcelFormattingSettings
{
    public string FontName { get; set; } = string.Empty;
    public int FontSize { get; set; } = 10;
    public string DateFormat { get; set; } = string.Empty;
}
