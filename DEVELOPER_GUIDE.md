# RapidZ Developer Guide

## Table of Contents
1. [Project Overview](#project-overview)
2. [Project Structure](#project-structure)
3. [Architecture Overview](#architecture-overview)
4. [Core Components](#core-components)
5. [Development Guidelines](#development-guidelines)
6. [Coding Standards](#coding-standards)
7. [Database Integration](#database-integration)
8. [Testing Strategy](#testing-strategy)
9. [Performance Considerations](#performance-considerations)
10. [Deployment](#deployment)

## Project Overview

RapidZ is a comprehensive data export/import application built with Avalonia UI and .NET. It provides robust functionality for processing database operations with real-time monitoring, validation, and Excel file generation capabilities.

## Project Structure

```
RapidZ/
├── Assets/                          # Application resources
│   ├── Images/                      # Icons and image files
│   │   ├── android-chrome-192x192.png
│   │   ├── android-chrome-512x512.png
│   │   ├── apple-touch-icon.png
│   │   ├── favicon-16x16.png
│   │   ├── favicon-32x32.png
│   │   ├── favicon.ico
│   │   └── site.webmanifest
│   └── Styles/                     # Application styling
│       └── GlobalStyles.axaml
├── Config/                          # Configuration files
│   ├── ExcelFormatSettings.cs
│   ├── ExportExcelFormatSettings.cs
│   ├── ExportExcelFormatSettings.json
│   ├── ImportExcelFormatSettings.cs
│   ├── ImportExcelFormatSettings.json
│   ├── database.appsettings.json
│   ├── export.appsettings.json
│   └── import.appsettings.json
├── Core/                            # Core business logic
│   ├── Cancellation/               # Cancellation management
│   │   ├── CancellationCleanupHelper.cs
│   │   ├── CancellationManager.cs
│   │   └── ICancellationManager.cs
│   ├── Controllers/                 # Application controllers
│   │   ├── ExportController.cs
│   │   ├── IExportController.cs
│   │   ├── IImportController.cs
│   │   └── ImportController.cs
│   ├── DataAccess/                  # Database access layer
│   │   ├── ExportDataAccess.cs
│   │   └── ImportDataAccess.cs
│   ├── Database/                    # Database utilities
│   │   ├── DatabaseObjectValidator.cs
│   │   └── SharedDatabaseSettings.cs
│   ├── Helpers/                     # Utility helpers
│   │   ├── BaseFileNameHelper.cs
│   │   ├── DateHelper.cs
│   │   ├── Export_FileNameHelper.cs
│   │   └── Import_FileNameHelper.cs
│   ├── Logging/                     # Logging framework
│   │   ├── Abstractions/
│   │   ├── Core/
│   │   ├── Enums/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Utilities/
│   ├── Models/                      # Data models and DTOs
│   │   ├── DbObjects.cs
│   │   ├── ExportInputs.cs
│   │   ├── ExportSettings.cs
│   │   ├── ImportInputs.cs
│   │   ├── ImportSettings.cs
│   │   └── ProcessingResult.cs
│   ├── Parameters/                  # Parameter management
│   │   ├── BaseParameterHelper.cs
│   │   ├── Export/
│   │   └── Import/
│   ├── Services/                    # Business services
│   │   ├── ConfigurationCacheService.cs
│   │   ├── ConfigurationService.cs
│   │   ├── DatabaseConnectionService.cs
│   │   ├── DatabaseService.cs
│   │   ├── DialogService.cs
│   │   ├── ILogParserService.cs
│   │   ├── IResultProcessorService.cs
│   │   ├── IValidationService.cs
│   │   ├── LogParserService.cs
│   │   ├── OperationalConnectionManager.cs
│   │   ├── PathValidationService.cs
│   │   ├── ResultProcessorService.cs
│   │   ├── ServiceContainer.cs
│   │   ├── UIActionService.cs
│   │   └── ValidationService.cs
│   ├── Utilities/                   # General utilities
│   └── Validation/                  # Validation framework
│       ├── IParameterValidator.cs
│       └── ParameterValidator.cs
├── Features/                        # Feature-specific modules
│   ├── Common/                      # Shared feature components
│   │   └── ViewModels/
│   ├── Export/                      # Export functionality
│   │   ├── ExportExcelService.cs
│   │   ├── ExportSettings.cs
│   │   └── Services/
│   ├── Import/                      # Import functionality
│   │   ├── ImportExcelService.cs
│   │   ├── ImportSettings.cs
│   │   └── Services/
│   └── Monitoring/                  # Real-time monitoring
│       ├── Models/
│       └── Services/
├── Views/                           # UI components and view models
│   ├── Controls/                    # Custom UI controls
│   │   ├── FilterControl.axaml
│   │   ├── FilterControl.axaml.cs
│   │   ├── FooterControl.axaml
│   │   ├── FooterControl.axaml.cs
│   │   ├── InputFieldsControl.axaml
│   │   ├── InputFieldsControl.axaml.cs
│   │   ├── LoadingControl.axaml
│   │   ├── LoadingControl.axaml.cs
│   │   ├── MessageBox.axaml
│   │   ├── MessageBox.axaml.cs
│   │   ├── ProcessingCompleteDialog.axaml
│   │   ├── ProcessingCompleteDialog.axaml.cs
│   │   ├── StatusPanelControl.axaml
│   │   ├── StatusPanelControl.axaml.cs
│   │   └── YearMonthPicker.cs
│   ├── Converters/                  # Value converters
│   │   ├── BoolToValidationBrushConverter.cs
│   │   ├── ColorDarkenConverter.cs
│   │   ├── ConnectionConverters.cs
│   │   ├── PercentageToWidthConverter.cs
│   │   ├── ProgressToIndeterminateConverter.cs
│   │   ├── StatusToIconConverter.cs
│   │   └── StringEqualsConverter.cs
│   ├── Models/                      # View-specific models
│   │   ├── ConnectionInfo.cs
│   │   ├── ExportDataFilter.cs
│   │   └── SystemStatus.cs
│   ├── ViewModels/                  # View models
│   │   ├── ImportViewModel.cs
│   │   ├── MainViewModel.cs
│   │   └── ViewModelBase.cs
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── RapidZ.csproj
└── app.manifest
```

## Architecture Overview

RapidZ follows a layered architecture pattern with clear separation of concerns:

### 1. Presentation Layer (Views/)
- **Views**: AXAML files and code-behind for UI components
- **ViewModels**: MVVM pattern implementation
- **Controls**: Reusable UI components
- **Converters**: Data binding value converters

### 2. Business Logic Layer (Core/)
- **Controllers**: Orchestrate business operations
- **Services**: Implement business logic and external integrations
- **Models**: Data transfer objects and business entities
- **Validation**: Input and business rule validation

### 3. Data Access Layer (Core/DataAccess/)
- **DataAccess**: Database operations and queries
- **Database**: Database utilities and configuration

### 4. Feature Modules (Features/)
- **Export**: Export-specific functionality
- **Import**: Import-specific functionality
- **Monitoring**: Real-time monitoring capabilities
- **Common**: Shared feature components

### 5. Infrastructure (Core/Services/)
- **ServiceContainer**: Dependency injection container
- **Configuration**: Application configuration management
- **Logging**: Structured logging implementation
- **Utilities**: Cross-cutting concerns

## Core Components

### Controllers

#### ExportController
- Manages export operations
- Orchestrates data processing workflows
- Handles cancellation and error scenarios

#### ImportController
- Handles import workflows
- Manages validation and processing
- Coordinates with database services

### Services

#### DatabaseService
- Core database operations
- Connection management
- Query execution and result processing

#### ValidationService
- Input parameter validation
- Business rule enforcement
- Error message generation

#### ConfigurationService
- Application settings management
- Configuration file loading
- Runtime configuration updates

#### MonitoringService
- Real-time progress tracking
- Performance metrics collection
- Status reporting

### Data Access

#### ExportDataAccess
- Export-specific database operations
- EXPDATA view queries
- Export stored procedure execution

#### ImportDataAccess
- Import-specific database queries
- IMPDATA view operations
- Import stored procedure management

## Development Guidelines

### Project Setup

1. **Prerequisites**
   - .NET 8.0 SDK
   - Visual Studio 2022 or JetBrains Rider
   - SQL Server (for database operations)

2. **Clone and Build**
   ```bash
   git clone <repository-url>
   cd RapidZ
   dotnet restore
   dotnet build
   ```

3. **Configuration**
   - Update database connection strings in `Config/database.appsettings.json`
   - Configure export settings in `Config/export.appsettings.json`
   - Configure import settings in `Config/import.appsettings.json`

### Adding New Features

1. **Feature Module Structure**
   ```
   Features/
   └── NewFeature/
       ├── Services/
       ├── Models/
       └── ViewModels/
   ```

2. **Service Registration**
   - Register services in `ServiceContainer.cs`
   - Follow dependency injection patterns
   - Implement appropriate interfaces

3. **UI Components**
   - Create views in `Views/Controls/`
   - Implement view models in `Views/ViewModels/`
   - Follow MVVM patterns

### Testing Strategy

1. **Unit Tests**
   - Test business logic in isolation
   - Mock external dependencies
   - Focus on Core/ components

2. **Integration Tests**
   - Test database operations
   - Validate configuration loading
   - Test service interactions

3. **UI Tests**
   - Test view model behavior
   - Validate data binding
   - Test user interactions

## Coding Standards

### Naming Conventions

```csharp
// Classes: PascalCase
public class ExportController { }

// Interfaces: PascalCase with 'I' prefix
public interface IExportController { }

// Methods: PascalCase
public async Task RunAsync() { }

// Properties: PascalCase
public string ConnectionString { get; set; }

// Fields: camelCase with underscore prefix
private readonly ILogger _logger;

// Parameters: camelCase
public void ProcessData(ExportInputs exportInputs) { }

// Local variables: camelCase
var validationResult = ValidateInputs();

// Constants: PascalCase
public const int MaxRetryAttempts = 3;

// Enums: PascalCase
public enum OperationStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
```

### Documentation Standards

```csharp
/// <summary>
/// Processes export operations asynchronously with the specified parameters.
/// </summary>
/// <param name="exportInputs">The export input parameters containing filters and date range.</param>
/// <param name="cancellationToken">Token to cancel the operation if needed.</param>
/// <param name="selectedView">The database view to use for data retrieval.</param>
/// <param name="selectedStoredProcedure">The stored procedure to execute for data processing.</param>
/// <param name="customOutputPath">Optional custom path for output files. Uses default if null.</param>
/// <returns>A task representing the asynchronous operation.</returns>
/// <exception cref="ValidationException">Thrown when input validation fails.</exception>
/// <exception cref="DatabaseException">Thrown when database operations fail.</exception>
/// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
public async Task RunAsync(
    ExportInputs exportInputs,
    CancellationToken cancellationToken,
    string selectedView,
    string selectedStoredProcedure,
    string? customOutputPath = null)
{
    // Implementation
}
```

### Async/Await Best Practices

```csharp
// Good: ConfigureAwait(false) for library code
public async Task<ExportResult> ProcessExportAsync(ExportInputs inputs)
{
    var data = await GetDataAsync(inputs).ConfigureAwait(false);
    var result = await ProcessDataAsync(data).ConfigureAwait(false);
    return result;
}

// Good: Parallel processing for independent operations
public async Task ProcessMultipleExportsAsync(List<ExportInputs> exports)
{
    var tasks = exports.Select(async export =>
    {
        try
        {
            return await ProcessExportAsync(export).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process export: {Export}", export);
            return null;
        }
    });
    
    var results = await Task.WhenAll(tasks).ConfigureAwait(false);
    var successfulResults = results.Where(r => r != null).ToList();
}

// Good: Cancellation token propagation
public async Task<T> ExecuteWithTimeoutAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    TimeSpan timeout,
    CancellationToken cancellationToken = default)
{
    using var timeoutCts = new CancellationTokenSource(timeout);
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, timeoutCts.Token);
    
    return await operation(combinedCts.Token).ConfigureAwait(false);
}
```

### Error Handling

```csharp
// Good: Specific exception handling
try
{
    await ProcessDataAsync();
}
catch (SqlException ex) when (ex.Number == -2) // Timeout
{
    _logger.LogWarning("Database timeout occurred, retrying...");
    await RetryOperationAsync();
}
catch (ValidationException ex)
{
    _logger.LogError(ex, "Validation failed: {Errors}", ex.ValidationResult.Errors);
    throw; // Re-throw to let caller handle
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during data processing");
    throw new DataProcessingException("Failed to process data", ex);
}
```

### Resource Management

```csharp
// Good: Using using statement
using var connection = await GetConnectionAsync();
// Use connection - automatically disposed

// Good: Using object pooling for frequently created objects
public class ExportInputsPool
{
    private static readonly ObjectPool<ExportInputs> Pool = 
        new DefaultObjectPool<ExportInputs>(new ExportInputsPoolPolicy());
    
    public static ExportInputs Get() => Pool.Get();
    public static void Return(ExportInputs inputs) => Pool.Return(inputs);
}
```

## Database Integration

### Required Database Objects

#### Export Views
- `EXPDATA`: Main export data view
- `EXPDATA_DETAILED`: Detailed export information
- `EXPDATA_SUMMARY`: Summary export data

#### Export Stored Procedures
- `ExportData_New1`: Primary export data procedure
- `ExportData_Detailed`: Detailed export processing
- `ExportData_Summary`: Summary export generation

#### Import Views
- `IMPDATA`: Main import data view
- `IMPDATA_DETAILED`: Detailed import information
- `IMPDATA_SUMMARY`: Summary import data

#### Import Stored Procedures
- `ImportJNPTData_New1`: Primary import data procedure
- `ImportJNPTData_Detailed`: Detailed import processing
- `ImportJNPTData_Summary`: Summary import generation

### Connection Management

```csharp
// Use OperationalConnectionManager for database connections
var connectionManager = ServiceContainer.GetService<OperationalConnectionManager>();
using var connection = await connectionManager.GetConnectionAsync();
```

## Performance Considerations

### Optimization Tips
1. **Database Indexing**: Ensure proper indexes on filtered columns
2. **Connection Pooling**: Configure appropriate connection pool settings
3. **Memory Management**: Monitor memory usage for large datasets
4. **Batch Size**: Adjust processing batch sizes based on system resources
5. **Timeout Settings**: Configure appropriate command timeouts

### Monitoring
- Real-time progress tracking via MonitoringService
- Performance metrics collection
- Error rate monitoring
- Resource utilization tracking

## Deployment

### Build Configuration

```xml
<!-- Release configuration in RapidZ.csproj -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <Optimize>true</Optimize>
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

### Deployment Steps

1. **Build Release**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. **Configuration**
   - Update production connection strings
   - Configure logging paths
   - Set appropriate timeout values

3. **Database Setup**
   - Ensure required views and stored procedures exist
   - Verify user permissions
   - Test database connectivity

### Troubleshooting

#### Common Issues

1. **Database Connection Errors**
   - Verify connection string configuration
   - Check SQL Server accessibility
   - Validate user permissions
   - Review firewall settings

2. **Excel Generation Issues**
   - Ensure sufficient disk space
   - Check file path permissions
   - Verify Excel format settings

3. **Performance Issues**
   - Monitor memory usage
   - Check database query performance
   - Review batch size settings
   - Analyze log files for bottlenecks

---

*This guide is maintained by the RapidZ development team. For questions or contributions, please refer to the project repository.*