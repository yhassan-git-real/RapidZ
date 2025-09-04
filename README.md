# RapidZ - TradeData || Indian Edition

![RapidZ Logo](RapidZ/Assets/Images/favicon.ico)

## Overview

RapidZ is a comprehensive trade data management application built with Avalonia UI and .NET 8. It provides powerful tools for importing and exporting trade data from SQL Server databases to Excel files, with advanced filtering capabilities and real-time monitoring.

## Features

### Export Operations
- **Database Objects**: Dedicated EXPDATA views and ExportData stored procedures
- **Excel Formatting**: Export-specific date formatting (column 3) and text formatting (columns 1,2,4)
- **File Naming**: Files generated with "EXP.xlsx" suffix using Export_FileNameHelper
- **Output Directory**: Dedicated EXPORT_Excel folder for all export files
- **Logging**: Export-specific log files with "Export_Log" prefix
- **Parameter Focus**: Exporter-centric filtering and data extraction

### Import Operations
- **Database Objects**: Dedicated IMPDATA views and ImportJNPTData stored procedures
- **Excel Formatting**: Import-specific date formatting (column 2) and text formatting (columns 1,3,4)
- **File Naming**: Files generated with "IMP.xlsx" suffix using Import_FileNameHelper
- **Output Directory**: Dedicated IMPORT_Excel folder for all import files
- **Logging**: Import-specific log files with "Import_Log" prefix
- **Parameter Focus**: Importer-centric filtering and data extraction

### Shared Functionality
- **Real-time Monitoring**: Track operation progress with live status updates for both export and import
- **Configurable Filtering**: Filter data by date range, ports, HS codes, products, and companies
- **Excel Generation**: Create formatted Excel workbooks with Data, Summary, Parameters, and Metadata worksheets
- **Database Integration**: Connect to SQL Server with configurable connection strings
- **Cancellation Support**: Cancel long-running operations gracefully
- **Validation**: Comprehensive parameter and database object validation
- **Modern UI**: Beautiful Avalonia-based interface with Material Design icons

## Technology Stack

- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.1.3
- **Database**: Microsoft SQL Server
- **Excel Processing**: EPPlus 7.0.2
- **Logging**: Serilog with file and console sinks
- **Architecture**: MVVM with ReactiveUI
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## System Requirements

- **Operating System**: Windows 10/11, macOS, or Linux
- **.NET Runtime**: .NET 8.0 or later
- **Database**: Microsoft SQL Server (local or remote)
- **Memory**: Minimum 4GB RAM (8GB recommended for large datasets)
- **Storage**: 500MB free space for application and logs

## Installation

### Prerequisites
1. Install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Ensure SQL Server access with appropriate permissions
3. Configure database connection settings

### Setup Steps
1. Clone or download the RapidZ application
2. Configure database connection in `Config/database.appsettings.json`
3. Update export/import settings in respective configuration files
4. Build and run the application

```bash
dotnet build RapidZ.sln
dotnet run --project RapidZ/RapidZ.csproj
```

## Configuration

### Database Configuration
Edit `Config/database.appsettings.json`:
```json
{
  "DatabaseConfig": {
    "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;Encrypt=false;",
    "CommandTimeoutSeconds": 3600,
    "LogDirectory": "F:\\RapidZ\\Logs"
  }
}
```

### Export Configuration
Edit `Config/export.appsettings.json`:
```json
{
  "ExportSettings": {
    "Operation": {
      "StoredProcedureName": "ExportData_New1",
      "ViewName": "EXPDATA",
      "OrderByColumn": "sb_DATE",
      "WorksheetName": "Export Data"
    },
    "Files": {
      "OutputDirectory": "F:\\RapidZ\\EXPORT_Excel"
    },
    "Logging": {
      "OperationLabel": "Excel Export Generation",
      "LogFilePrefix": "Export_Log"
    },
    "ExportObjects": {
      "DefaultViewName": "EXPDATA",
      "DefaultStoredProcedureName": "ExportData_New1",
      "Views": [
        {"Name": "EXPDATA", "OrderByColumn": "sb_DATE"},
        {"Name": "EXPDATA_DETAILED", "OrderByColumn": "sb_DATE"},
        {"Name": "EXPDATA_SUMMARY", "OrderByColumn": "sb_DATE"}
      ],
      "StoredProcedures": [
        {"Name": "ExportData_New1"},
        {"Name": "ExportData_Detailed"},
        {"Name": "ExportData_Summary"}
      ]
    }
  }
}
```

### Import Configuration
Edit `Config/import.appsettings.json`:
```json
{
  "ImportSettings": {
    "Database": {
      "StoredProcedureName": "ImportJNPTData_New1",
      "ViewName": "IMPDATA",
      "OrderByColumn": "DATE",
      "WorksheetName": "Import Data"
    },
    "Files": {
      "OutputDirectory": "F:\\RapidZ\\IMPORT_Excel",
      "FileSuffix": "IMP"
    },
    "Logging": {
      "OperationLabel": "Excel Import Generation",
      "LogFilePrefix": "Import_Log"
    },
    "ImportObjects": {
      "DefaultViewName": "IMPDATA",
      "DefaultStoredProcedureName": "ImportJNPTData_New1",
      "Views": [
        {"Name": "IMPDATA", "OrderByColumn": "DATE"},
        {"Name": "IMPDATA_DETAILED", "OrderByColumn": "DATE"},
        {"Name": "IMPDATA_SUMMARY", "OrderByColumn": "DATE"}
      ],
      "StoredProcedures": [
        {"Name": "ImportJNPTData_New1"},
        {"Name": "ImportJNPTData_Detailed"},
        {"Name": "ImportJNPTData_Summary"}
      ]
    }
  }
}
```

## Usage

### Export Operations
1. Launch RapidZ application
2. Select "Export" tab
3. Configure export-specific filter parameters:
   - Date range (From/To months in YYYYMM format)
   - Ports, HS Codes, Products
   - **Exporters** (company names for export operations)
   - IECs, Countries, Foreign Names
4. Select from export database objects:
   - **Views**: EXPDATA, EXPDATA_DETAILED, EXPDATA_SUMMARY
   - **Stored Procedures**: ExportData_New1, ExportData_Detailed, ExportData_Summary
5. Click "Start Export" to begin processing
6. Monitor progress in real-time
7. Generated Excel files use export-specific formatting:
   - **File naming**: `{core}_{monthRange}EXP.xlsx`
   - **Excel formatting**: Date columns [3], Text columns [1,2,4]
   - **Output directory**: `F:\RapidZ\EXPORT_Excel`

### Import Operations
1. Switch to "Import" tab
2. Configure import-specific filter parameters:
   - Date range (From/To months in YYYYMM format)
   - Ports, HS Codes, Products
   - **Importers** (company names for import operations)
   - IECs, Countries, Foreign Names
3. Select from import database objects:
   - **Views**: IMPDATA, IMPDATA_DETAILED, IMPDATA_SUMMARY
   - **Stored Procedures**: ImportJNPTData_New1, ImportJNPTData_Detailed, ImportJNPTData_Summary
4. Click "Start Import" to begin processing
5. Monitor progress in real-time
6. Generated Excel files use import-specific formatting:
   - **File naming**: `{core}_{monthRange}IMP.xlsx`
   - **Excel formatting**: Date columns [2], Text columns [1,3,4]
   - **Output directory**: `F:\RapidZ\IMPORT_Excel`

### Key Differences Between Export and Import

#### Database Objects
- **Export**: Uses EXPDATA views and ExportData procedures with "sb_DATE" ordering
- **Import**: Uses IMPDATA views and ImportJNPTData procedures with "DATE" ordering

#### Excel Formatting
- **Export**: Date formatting on column 3, text formatting on columns 1,2,4
- **Import**: Date formatting on column 2, text formatting on columns 1,3,4

#### File Naming
- **Export**: Files end with "EXP.xlsx" suffix
- **Import**: Files end with "IMP.xlsx" suffix (configurable via FileSuffix setting)

#### Parameter Focus
- **Export**: Focuses on **Exporters** and export-related data
- **Import**: Focuses on **Importers** and import-related data

#### Logging
- **Export**: Uses "Export_Log" prefix for log files
- **Import**: Uses "Import_Log" prefix for log files

## Architecture

### Project Structure
```
RapidZ/
├── Assets/                 # Images, styles, and resources
├── Config/                 # Configuration files
├── Core/                   # Core business logic
│   ├── Controllers/        # Application controllers
│   ├── DataAccess/         # Database access layer
│   ├── Models/             # Data models and DTOs
│   ├── Services/           # Business services
│   └── Validation/         # Validation framework
├── Features/               # Feature-specific modules
│   ├── Export/             # Export functionality
│   ├── Import/             # Import functionality
│   └── Monitoring/         # Real-time monitoring
└── Views/                  # UI components and view models
```

### Key Components

#### Controllers
- **ExportController**: Manages export operations and orchestrates data processing
- **ImportController**: Handles import workflows and validation

#### Services
- **ExportExcelService**: Excel file generation and formatting
- **ImportExcelService**: Import data processing
- **ValidationService**: Input and database validation
- **MonitoringService**: Real-time progress tracking
- **DatabaseService**: Database connectivity and operations

#### Data Access
- **ExportDataAccess**: Export-specific database operations
- **ImportDataAccess**: Import-specific database queries
- **OperationalConnectionManager**: Database connection management

## Database Schema

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

## Logging

RapidZ uses Serilog for comprehensive logging:

- **Console Logging**: Real-time console output
- **File Logging**: Persistent log files in configured directory
- **Structured Logging**: JSON-formatted logs for analysis
- **Log Levels**: Debug, Information, Warning, Error, Fatal

### Log File Locations
- Export logs: `{LogDirectory}/Export_Log_{timestamp}.txt`
- Import logs: `{LogDirectory}/Import_Log_{timestamp}.txt`
- Application logs: `{LogDirectory}/RapidZ_{timestamp}.log`

## Performance Considerations

### Optimization Tips
1. **Database Indexing**: Ensure proper indexes on filtered columns
2. **Connection Pooling**: Configure appropriate connection pool settings
3. **Memory Management**: Monitor memory usage for large datasets
4. **Batch Size**: Adjust processing batch sizes based on system resources
5. **Timeout Settings**: Configure appropriate command timeouts

### Monitoring
- Real-time progress tracking
- Performance metrics collection
- Error rate monitoring
- Resource utilization tracking

## Troubleshooting

### Common Issues

#### Database Connection Errors
- Verify connection string configuration
- Check SQL Server accessibility
- Validate user permissions
- Review firewall settings

#### Excel Generation Issues
- Ensure sufficient disk space
- Check output directory permissions
- Verify EPPlus license compliance
- Monitor memory usage for large datasets

#### Performance Issues
- Review database query performance
- Check system resource utilization
- Optimize filter parameters
- Consider data archiving strategies

### Error Codes
- **DB001**: Database connection failure
- **VAL001**: Validation error
- **EXP001**: Export operation failure
- **IMP001**: Import operation failure
- **FILE001**: File system error

## Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8.0 SDK
3. Restore NuGet packages: `dotnet restore`
4. Build solution: `dotnet build`
5. Run tests: `dotnet test`

### Code Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Implement proper error handling
- Write unit tests for new features

### Pull Request Process
1. Create feature branch from main
2. Implement changes with tests
3. Update documentation
4. Submit pull request with description
5. Address review feedback

## License

This project uses EPPlus under the NonCommercial license. For commercial use, please ensure proper EPPlus licensing.

## Support

For technical support and questions:
- Review this documentation
- Check application logs
- Consult troubleshooting section
- Contact development team

## Version History

### Current Version
- **Framework**: .NET 8.0
- **UI**: Avalonia 11.1.3
- **Database**: SQL Server integration
- **Excel**: EPPlus 7.0.2

---

**RapidZ TradeData Manager** - Efficient trade data processing and Excel generation solution.