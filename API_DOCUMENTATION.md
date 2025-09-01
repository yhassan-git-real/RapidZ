# RapidZ API Documentation

## Table of Contents
1. [Overview](#overview)
2. [Controllers](#controllers)
3. [Services](#services)
4. [Data Models](#data-models)
5. [Interfaces](#interfaces)
6. [Configuration Models](#configuration-models)
7. [Validation](#validation)
8. [Usage Examples](#usage-examples)

## Overview

RapidZ provides a comprehensive API for managing distinct trade data import and export operations. The application follows a layered architecture with clear separation between controllers, services, data access, and validation layers, with dedicated components for each operation type.

### Architecture Layers
- **Controllers**: Handle business logic with separate controllers for import (IImportController) and export (IExportController) operations
- **Services**: Provide specialized functionality with operation-specific services (ExportExcelService, ImportExcelService, validation, monitoring)
- **Data Access**: Manage database operations with distinct database objects (EXPDATA vs IMPDATA views, ExportData vs ImportJNPTData procedures)
- **Models**: Define data structures and DTOs with separate input models (ExportInputs, ImportInputs) and configuration models
- **Validation**: Ensure data integrity with operation-specific validation services (ExportObjectValidationService, ImportObjectValidationService)

### Key Architectural Distinctions

#### Export Operations
- **Database Objects**: EXPDATA views, ExportData stored procedures
- **Excel Services**: ExportExcelService with export-specific formatting
- **File Helpers**: Export_FileNameHelper for EXP.xlsx file naming
- **Configuration**: ExportSettings, ExportExcelFormatSettings
- **Validation**: ExportObjectValidationService

#### Import Operations
- **Database Objects**: IMPDATA views, ImportJNPTData stored procedures
- **Excel Services**: ImportExcelService with import-specific formatting
- **File Helpers**: Import_FileNameHelper for IMP.xlsx file naming
- **Configuration**: ImportSettings, ImportExcelFormatSettings
- **Validation**: ImportObjectValidationService

## Controllers

### IExportController

Interface for export controller operations.

#### Methods

##### RunAsync
```csharp
Task RunAsync(
    ExportInputs exportInputs, 
    CancellationToken cancellationToken, 
    string selectedView, 
    string selectedStoredProcedure, 
    string? customOutputPath = null
)
```

**Description**: Runs the export process asynchronously.

**Parameters**:
- `exportInputs` (ExportInputs): The export input parameters
- `cancellationToken` (CancellationToken): Cancellation token for the operation
- `selectedView` (string): The selected database view name
- `selectedStoredProcedure` (string): The selected stored procedure name
- `customOutputPath` (string?, optional): Custom output path for generated files

**Returns**: Task representing the async operation

### IImportController

Interface for import controller operations.

#### Methods

##### RunAsync
```csharp
Task RunAsync(
    ImportInputs importInputs, 
    CancellationToken cancellationToken, 
    string selectedView, 
    string selectedStoredProcedure, 
    string? customOutputPath = null
)
```

**Description**: Runs the import process asynchronously.

**Parameters**:
- `importInputs` (ImportInputs): The import input parameters
- `cancellationToken` (CancellationToken): Cancellation token for the operation
- `selectedView` (string): The selected database view name
- `selectedStoredProcedure` (string): The selected stored procedure name
- `customOutputPath` (string?, optional): Custom output directory path

**Returns**: Task representing the async operation

## Services

RapidZ provides specialized services for handling distinct import and export operations. Each operation type has dedicated services for Excel generation, validation, and file management, ensuring proper separation of concerns and operation-specific functionality.

### Core Service Architecture

#### Export-Specific Services
- **ExportExcelService**: Handles Excel file generation for export data with export-specific formatting
- **ExportObjectValidationService**: Validates export database objects and configurations
- **Export_FileNameHelper**: Manages export file naming conventions (EXP.xlsx suffix)
- **ExportParameterHelper**: Handles export-specific parameter validation and mapping

#### Import-Specific Services
- **ImportExcelService**: Handles Excel file generation for import data with import-specific formatting
- **ImportObjectValidationService**: Validates import database objects and configurations
- **Import_FileNameHelper**: Manages import file naming conventions (IMP.xlsx suffix)
- **ImportParameterHelper**: Handles import-specific parameter validation and mapping

#### Shared Services
- **DatabaseConnectionService**: Manages database connectivity and monitoring
- **MonitoringService**: Provides real-time operation monitoring
- **CancellationManager**: Handles operation cancellation across both import and export

### IValidationService

Interface for validation service operations.

#### Methods

##### ValidateExportOperation
```csharp
ValidationResult ValidateExportOperation(
    ExportInputs exportInputs, 
    string selectedView, 
    string selectedStoredProcedure
)
```

**Description**: Validates export inputs and database objects.

**Parameters**:
- `exportInputs` (ExportInputs): Export input parameters to validate
- `selectedView` (string): Selected database view name
- `selectedStoredProcedure` (string): Selected stored procedure name

**Returns**: ValidationResult with validation status and error messages

##### ValidateImportOperation
```csharp
ValidationResult ValidateImportOperation(
    ImportInputs importInputs, 
    string selectedView, 
    string selectedStoredProcedure
)
```

**Description**: Validates import inputs and database objects.

**Parameters**:
- `importInputs` (ImportInputs): Import input parameters to validate
- `selectedView` (string): Selected database view name
- `selectedStoredProcedure` (string): Selected stored procedure name

**Returns**: ValidationResult with validation status and error messages

### IParameterValidator

Interface for parameter validation operations.

#### Methods

##### ValidateExport
```csharp
ExportParameterHelper.ValidationResult ValidateExport(ExportInputs exportInputs)
```

**Description**: Validates export input parameters.

**Parameters**:
- `exportInputs` (ExportInputs): The export input parameters to validate

**Returns**: Validation result with success/failure and error messages

##### ValidateImport
```csharp
ImportParameterHelper.ValidationResult ValidateImport(ImportInputs importInputs)
```

**Description**: Validates import input parameters.

**Parameters**:
- `importInputs` (ImportInputs): The import input parameters to validate

**Returns**: Validation result with success/failure and error messages

### DatabaseConnectionService

Manages database connection monitoring and status.

#### Properties

- `ConnectionInfo` (DatabaseConnectionInfo): Current connection information
- `IsConnected` (bool): Connection status
- `ServerName` (string): Database server name
- `DatabaseName` (string): Database name
- `UserAccount` (string): User account
- `ConnectionStatus` (string): Connection status text
- `StatusColor` (string): Status indicator color
- `LastChecked` (DateTime): Last connection check time
- `ResponseTime` (int): Connection response time in milliseconds

#### Methods

##### StartContinuousMonitoring
```csharp
void StartContinuousMonitoring()
```

**Description**: Starts continuous database connection monitoring.

##### StopContinuousMonitoring
```csharp
void StopContinuousMonitoring()
```

**Description**: Stops continuous database connection monitoring.

##### PauseConnectionChecks
```csharp
void PauseConnectionChecks()
```

**Description**: Temporarily pauses connection checks during operations.

##### ResumeConnectionChecks
```csharp
void ResumeConnectionChecks()
```

**Description**: Resumes connection checks after operations complete.

### DatabaseService

Handles database operations for export and import data.

#### Properties

- `IsConnected` (bool): Database connection status
- `ConnectionStatus` (string): Connection status description

#### Methods

##### GetImportData
```csharp
Tuple<SqlConnection, SqlDataReader, long> GetImportData(
    string fromMonth, 
    string toMonth, 
    string hsCode, 
    string product, 
    string iec, 
    string importer, 
    string country, 
    string name, 
    string port, 
    CancellationToken cancellationToken = default, 
    string? viewName = null, 
    string? storedProcedureName = null
)
```

**Description**: Retrieves import data using the specified parameters.

**Parameters**:
- `fromMonth` (string): Start month for data retrieval
- `toMonth` (string): End month for data retrieval
- `hsCode` (string): HS code filter
- `product` (string): Product filter
- `iec` (string): IEC filter
- `importer` (string): Importer filter
- `country` (string): Country filter
- `name` (string): Name filter
- `port` (string): Port filter
- `cancellationToken` (CancellationToken, optional): Cancellation token
- `viewName` (string?, optional): Custom view name
- `storedProcedureName` (string?, optional): Custom stored procedure name

**Returns**: Tuple containing SqlConnection, SqlDataReader, and record count

### ServiceContainer

Container for managing all application services and their dependencies.

#### Properties

##### Core Services
- `ExcelService` (ExportExcelService): Excel generation service for exports
- `ImportService` (ImportExcelService): Excel generation service for imports
- `CancellationManager` (ICancellationManager): Cancellation management
- `MonitoringService` (MonitoringService): System monitoring and status

##### Validation Services
- `ExportObjectValidationService` (ExportObjectValidationService): Export object validation
- `ImportObjectValidationService` (ImportObjectValidationService): Import object validation
- `ParameterValidator` (IParameterValidator): Parameter validation
- `DatabaseObjectValidator` (DatabaseObjectValidator): Database object validation
- `ValidationService` (IValidationService): General validation service
- `ResultProcessorService` (IResultProcessorService): Result processing
- `PathValidationService` (PathValidationService): File path validation

##### Controllers
- `ExportController` (IExportController): Export operations controller
- `ImportController` (IImportController): Import operations controller

##### UI Services
- `UIActionService` (IUIActionService): UI action coordination

##### View Models
- `ExportDbObjectViewModel` (DbObjectSelectorViewModel): Export database object selection
- `ImportDbObjectViewModel` (DbObjectSelectorViewModel): Import database object selection

## Data Models

RapidZ uses distinct data models for import and export operations, with key differences in company-focused parameters that reflect the different perspectives of each operation type.

### ExportInputs

DTO for export input parameters, focusing on **Exporters** (domestic companies selling goods abroad).

```csharp
public record ExportInputs(
    string FromMonth, 
    string ToMonth, 
    List<string> Ports, 
    List<string> HSCodes, 
    List<string> Products,
    List<string> Exporters,  // Domestic companies exporting goods
    List<string> IECs, 
    List<string> ForeignCountries, 
    List<string> ForeignNames
);
```

**Properties**:
- `FromMonth` (string): Start month for export data
- `ToMonth` (string): End month for export data
- `Ports` (List<string>): List of port filters
- `HSCodes` (List<string>): List of HS code filters
- `Products` (List<string>): List of product filters
- `Exporters` (List<string>): **Key Focus** - List of domestic exporter company filters
- `IECs` (List<string>): List of IEC (Importer Exporter Code) filters
- `ForeignCountries` (List<string>): List of destination country filters
- `ForeignNames` (List<string>): List of foreign buyer/recipient filters

### ImportInputs

DTO for import input parameters, focusing on **Importers** (domestic companies buying goods from abroad).

```csharp
public record ImportInputs(
    string FromMonth, 
    string ToMonth, 
    List<string> Ports, 
    List<string> HSCodes, 
    List<string> Products,
    List<string> Importers,  // Domestic companies importing goods
    List<string> IECs, 
    List<string> ForeignCountries, 
    List<string> ForeignNames
);
```

**Properties**:
- `FromMonth` (string): Start month for import data
- `ToMonth` (string): End month for import data
- `Ports` (List<string>): List of port filters
- `HSCodes` (List<string>): List of HS code filters
- `Products` (List<string>): List of product filters
- `Importers` (List<string>): **Key Focus** - List of domestic importer company filters
- `IECs` (List<string>): List of IEC (Importer Exporter Code) filters
- `ForeignCountries` (List<string>): List of origin country filters
- `ForeignNames` (List<string>): List of foreign supplier/sender filters

### Key Model Differences

| Aspect | ExportInputs | ImportInputs |
|--------|--------------|-------------|
| **Company Focus** | `Exporters` (domestic sellers) | `Importers` (domestic buyers) |
| **Data Perspective** | Goods leaving the country | Goods entering the country |
| **Foreign Countries** | Destination markets | Origin/source countries |
| **Foreign Names** | International buyers | International suppliers |
| **Database Integration** | Works with EXPDATA views | Works with IMPDATA views |

### ValidationResult

Result of validation operation.

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Title { get; set; } = "Validation Error";
    public string[] Errors { get; set; } = Array.Empty<string>();
}
```

**Properties**:
- `IsValid` (bool): Whether validation passed
- `ErrorMessage` (string): Primary error message
- `Title` (string): Error title for display
- `Errors` (string[]): Array of detailed error messages

### DbObjectOption

Represents a database object option for dropdown selection.

```csharp
public class DbObjectOption
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string OrderByColumn { get; set; }
}
```

**Properties**:
- `Name` (string): The actual name of the database object
- `DisplayName` (string): The display name shown in the UI
- `OrderByColumn` (string): Optional order by column for views

### DbObjectPair

Represents a pair of database objects (View and Stored Procedure).

```csharp
public class DbObjectPair
{
    public string? ViewName { get; set; }
    public string? StoredProcedureName { get; set; }
}
```

**Properties**:
- `ViewName` (string?): The name of the view
- `StoredProcedureName` (string?): The name of the stored procedure

### DatabaseConnectionInfo

Information about database connection status.

```csharp
public class DatabaseConnectionInfo
{
    public string ServerName { get; set; } = "Unknown";
    public string DatabaseName { get; set; } = "Unknown";
    public string UserAccount { get; set; } = "Unknown";
    public string ConnectionStatus { get; set; } = "Disconnected";
    public string StatusColor { get; set; } = "#dc3545";
    public DateTime LastChecked { get; set; } = DateTime.Now;
    public int ResponseTime { get; set; } = 0;
}
```

**Properties**:
- `ServerName` (string): Database server name
- `DatabaseName` (string): Database name
- `UserAccount` (string): User account name
- `ConnectionStatus` (string): Connection status description
- `StatusColor` (string): Color code for status indicator
- `LastChecked` (DateTime): Last connection check timestamp
- `ResponseTime` (int): Response time in milliseconds

## Configuration Models

### ExportSettings

Configuration settings for export operations.

```csharp
public class ExportSettings
{
    public ExportOperationSettings Operation { get; set; }
    public ExportFileSettings Files { get; set; }
    public ExportLoggingSettings Logging { get; set; }
    public ExportObjectsSettings? ExportObjects { get; set; }
}
```

#### ExportOperationSettings
```csharp
public class ExportOperationSettings
{
    public string StoredProcedureName { get; set; }
    public string ViewName { get; set; }
    public string OrderByColumn { get; set; }
    public string WorksheetName { get; set; }
}
```

#### ExportFileSettings
```csharp
public class ExportFileSettings
{
    public string OutputDirectory { get; set; }
}
```

#### ExportLoggingSettings
```csharp
public class ExportLoggingSettings
{
    public string OperationLabel { get; set; }
    public string LogFilePrefix { get; set; }
    public string LogFileExtension { get; set; }
}
```

#### ExportObjectsSettings
```csharp
public class ExportObjectsSettings
{
    public string DefaultViewName { get; set; }
    public string DefaultStoredProcedureName { get; set; }
    public List<DbObjectOption> Views { get; set; }
    public List<DbObjectOption> StoredProcedures { get; set; }
}
```

### ImportSettings

Configuration settings for import operations.

```csharp
public class ImportSettings
{
    public ImportDatabaseSettings Database { get; set; }
    public ImportFileSettings Files { get; set; }
    public ImportLoggingSettings Logging { get; set; }
    public ImportObjectsSettings? ImportObjects { get; set; }
}
```

#### ImportDatabaseSettings
```csharp
public class ImportDatabaseSettings
{
    public string StoredProcedureName { get; set; }
    public string ViewName { get; set; }
    public string OrderByColumn { get; set; }
    public string WorksheetName { get; set; }
}
```

#### ImportFileSettings
```csharp
public class ImportFileSettings
{
    public string OutputDirectory { get; set; }
    public string FileSuffix { get; set; }
}
```

#### ImportLoggingSettings
```csharp
public class ImportLoggingSettings
{
    public string OperationLabel { get; set; }
    public string LogFilePrefix { get; set; }
    public string LogFileExtension { get; set; }
}
```

#### ImportObjectsSettings
```csharp
public class ImportObjectsSettings
{
    public string DefaultViewName { get; set; }
    public string DefaultStoredProcedureName { get; set; }
    public List<DbObjectOption> Views { get; set; }
    public List<DbObjectOption> StoredProcedures { get; set; }
}
```

## Interfaces

### IUIActionService

Interface for UI action coordination.

#### Events
- `BusyStateChanged` (Action<bool>): Fired when busy state changes

#### Methods

##### Initialize
```csharp
void Initialize(Window window)
```

**Description**: Initializes the service with the main window reference.

##### SetServiceContainer
```csharp
void SetServiceContainer(ServiceContainer serviceContainer)
```

**Description**: Sets the service container reference.

##### SetCurrentExportFilter
```csharp
void SetCurrentExportFilter(ExportDataFilter filter)
```

**Description**: Sets the current export filter data.

### IResultProcessorService

Interface for processing and tracking operation results.

#### Methods

##### ProcessResult
```csharp
void ProcessResult(OperationResult result)
```

**Description**: Processes operation results and updates counters.

### ICancellationManager

Interface for managing operation cancellation.

#### Methods

##### CreateCancellationToken
```csharp
CancellationToken CreateCancellationToken()
```

**Description**: Creates a new cancellation token for operations.

##### CancelCurrentOperation
```csharp
void CancelCurrentOperation()
```

**Description**: Cancels the currently running operation.

## Validation

### Export Object Validation

The `ExportObjectValidationService` provides methods to validate export database objects:

#### ValidateObjects
```csharp
bool ValidateObjects(string? viewName, string? storedProcedureName)
```

**Description**: Validates that the specified view and stored procedure exist in the configuration.

#### GetAvailableViews
```csharp
List<DbObjectOption> GetAvailableViews()
```

**Description**: Gets all available views from the configuration.

#### GetAvailableStoredProcedures
```csharp
List<DbObjectOption> GetAvailableStoredProcedures()
```

**Description**: Gets all available stored procedures from the configuration.

#### GetDefaultViewName
```csharp
string GetDefaultViewName()
```

**Description**: Gets the default view name from configuration.

#### GetDefaultStoredProcedureName
```csharp
string GetDefaultStoredProcedureName()
```

**Description**: Gets the default stored procedure name from configuration.

### Import Object Validation

The `ImportObjectValidationService` provides similar validation methods for import operations:

#### ValidateObjects
```csharp
bool ValidateObjects(string? viewName, string? storedProcedureName)
```

#### GetAvailableViews
```csharp
List<DbObjectOption> GetAvailableViews()
```

#### GetAvailableStoredProcedures
```csharp
List<DbObjectOption> GetAvailableStoredProcedures()
```

#### GetDefaultViewName
```csharp
string GetDefaultViewName()
```

#### GetDefaultStoredProcedureName
```csharp
string GetDefaultStoredProcedureName()
```

## Usage Examples

### Basic Export Operation

```csharp
// Create export inputs
var exportInputs = new ExportInputs(
    FromMonth: "202401",
    ToMonth: "202412",
    Ports: new List<string> { "INMAA1" },
    HSCodes: new List<string> { "1234567890" },
    Products: new List<string> { "Product1" },
    Exporters: new List<string> { "Exporter1" },
    IECs: new List<string> { "IEC123" },
    ForeignCountries: new List<string> { "USA" },
    ForeignNames: new List<string> { "Company1" }
);

// Validate inputs
var validationResult = validationService.ValidateExportOperation(
    exportInputs, 
    "EXPDATA", 
    "ExportData_New1"
);

if (validationResult.IsValid)
{
    // Run export operation
    var cancellationToken = new CancellationToken();
    await exportController.RunAsync(
        exportInputs, 
        cancellationToken, 
        "EXPDATA", 
        "ExportData_New1"
    );
}
else
{
    // Handle validation errors
    Console.WriteLine($"Validation failed: {validationResult.ErrorMessage}");
}
```

### Basic Import Operation

```csharp
// Create import inputs
var importInputs = new ImportInputs(
    FromMonth: "202401",
    ToMonth: "202412",
    Ports: new List<string> { "INJNP1" },
    HSCodes: new List<string> { "1234567890" },
    Products: new List<string> { "Product1" },
    Importers: new List<string> { "Importer1" },
    IECs: new List<string> { "IEC123" },
    ForeignCountries: new List<string> { "China" },
    ForeignNames: new List<string> { "Supplier1" }
);

// Validate inputs
var validationResult = validationService.ValidateImportOperation(
    importInputs, 
    "IMPDATA", 
    "ImportJNPTData_New1"
);

if (validationResult.IsValid)
{
    // Run import operation
    var cancellationToken = new CancellationToken();
    await importController.RunAsync(
        importInputs, 
        cancellationToken, 
        "IMPDATA", 
        "ImportJNPTData_New1"
    );
}
else
{
    // Handle validation errors
    Console.WriteLine($"Validation failed: {validationResult.ErrorMessage}");
}
```

### Database Connection Monitoring

```csharp
// Get database connection service
var connectionService = DatabaseConnectionService.Instance;

// Start monitoring
connectionService.StartContinuousMonitoring();

// Subscribe to property changes
connectionService.PropertyChanged += (sender, e) => {
    if (e.PropertyName == nameof(connectionService.ConnectionInfo))
    {
        var info = connectionService.ConnectionInfo;
        Console.WriteLine($"Connection Status: {info.ConnectionStatus}");
        Console.WriteLine($"Response Time: {info.ResponseTime}ms");
    }
};

// Stop monitoring when done
connectionService.StopContinuousMonitoring();
```

### Service Container Usage

```csharp
// Initialize service container
var serviceContainer = new ServiceContainer();

// Access services
var exportController = serviceContainer.ExportController;
var importController = serviceContainer.ImportController;
var validationService = serviceContainer.ValidationService;
var monitoringService = serviceContainer.MonitoringService;

// Use services for operations
var exportViews = serviceContainer.ExportObjectValidationService.GetAvailableViews();
var importViews = serviceContainer.ImportObjectValidationService.GetAvailableViews();
```

---

## Error Handling

All API methods follow standard .NET exception handling patterns:

- **ArgumentNullException**: Thrown when required parameters are null
- **InvalidOperationException**: Thrown when operations are called in invalid states
- **SqlException**: Thrown for database-related errors
- **OperationCanceledException**: Thrown when operations are cancelled

## Thread Safety

Most services are designed to be thread-safe for read operations. Write operations and state changes should be performed on the UI thread using the Dispatcher.

## Performance Considerations

- Database operations are performed asynchronously to avoid blocking the UI
- Large data sets are processed in batches to manage memory usage
- Connection pooling is used for database connections
- Cancellation tokens allow for responsive operation cancellation

---

*This documentation covers the core API components of RapidZ. For implementation details and advanced usage scenarios, refer to the source code and developer documentation.*