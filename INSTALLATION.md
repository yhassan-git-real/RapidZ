# RapidZ Installation and Setup Guide

## Table of Contents
1. [System Requirements](#system-requirements)
2. [Prerequisites](#prerequisites)
3. [Database Setup](#database-setup)
4. [Application Installation](#application-installation)
5. [Configuration](#configuration)
6. [First Run](#first-run)
7. [Verification](#verification)
8. [Troubleshooting](#troubleshooting)

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 (1903 or later), macOS 10.15+, or Linux (Ubuntu 18.04+)
- **Processor**: x64 compatible processor
- **Memory**: 4 GB RAM
- **Storage**: 1 GB available disk space
- **Network**: Internet connection for initial setup and database access

### Recommended Requirements
- **Memory**: 8 GB RAM or more
- **Storage**: 5 GB available disk space (for logs and Excel files)
- **Processor**: Multi-core processor for better performance

## Prerequisites

### 1. .NET 8.0 Runtime

#### Windows
1. Download .NET 8.0 Runtime from [Microsoft's official site](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Run the installer and follow the setup wizard
3. Verify installation:
   ```cmd
   dotnet --version
   ```

#### macOS
1. Download .NET 8.0 Runtime for macOS
2. Install the .pkg file
3. Verify installation:
   ```bash
   dotnet --version
   ```

#### Linux (Ubuntu/Debian)
```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0
```

### 2. SQL Server Access

Ensure you have access to a SQL Server instance with:
- **Server Name/IP**: Accessible SQL Server instance
- **Database**: Target database with trade data
- **Credentials**: Valid username and password with appropriate permissions
- **Permissions**: SELECT, EXECUTE permissions on required views and stored procedures

### 3. Required Database Objects

Ensure the following database objects exist:

#### Views
- `EXPDATA` - Main export data view
- `IMPDATA` - Main import data view
- `EXPDATA_DETAILED` - Detailed export view (optional)
- `IMPDATA_DETAILED` - Detailed import view (optional)

#### Stored Procedures
- `ExportData_New1` - Main export procedure
- `ImportJNPTData_New1` - Main import procedure

## Database Setup

### 1. Database Schema Verification

Connect to your SQL Server and verify the required objects exist:

```sql
-- Check for required views
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.VIEWS 
WHERE TABLE_NAME IN ('EXPDATA', 'IMPDATA', 'EXPDATA_DETAILED', 'IMPDATA_DETAILED');

-- Check for required stored procedures
SELECT ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE' 
AND ROUTINE_NAME IN ('ExportData_New1', 'ImportJNPTData_New1');
```

### 2. User Permissions

Ensure the application user has necessary permissions:

```sql
-- Grant SELECT permissions on views
GRANT SELECT ON EXPDATA TO [your_app_user];
GRANT SELECT ON IMPDATA TO [your_app_user];

-- Grant EXECUTE permissions on stored procedures
GRANT EXECUTE ON ExportData_New1 TO [your_app_user];
GRANT EXECUTE ON ImportJNPTData_New1 TO [your_app_user];
```

### 3. Connection Testing

Test database connectivity:

```sql
-- Test connection with application credentials
SQLCMD -S [server_name] -U [username] -P [password] -d [database_name] -Q "SELECT GETDATE()"
```

## Application Installation

### Option 1: Pre-built Release (Recommended)

1. **Download Release**
   - Download the latest RapidZ release package
   - Extract to desired installation directory (e.g., `C:\RapidZ`)

2. **Verify Files**
   Ensure the following files are present:
   ```
   RapidZ/
   ├── RapidZ.exe (Windows) or RapidZ (Linux/macOS)
   ├── RapidZ.dll
   ├── Config/
   │   ├── database.appsettings.json
   │   ├── export.appsettings.json
   │   └── import.appsettings.json
   └── Assets/
   ```

### Option 2: Build from Source

1. **Clone Repository**
   ```bash
   git clone [repository_url]
   cd RapidZ
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore RapidZ.sln
   ```

3. **Build Application**
   ```bash
   dotnet build RapidZ.sln --configuration Release
   ```

4. **Publish Application**
   ```bash
   dotnet publish RapidZ/RapidZ.csproj --configuration Release --output ./publish
   ```

## Configuration

### 1. Database Configuration

Edit `Config/database.appsettings.json`:

```json
{
  "DatabaseConfig": {
    "ConnectionString": "Server=YOUR_SERVER_NAME;Database=YOUR_DATABASE_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=true;Encrypt=false;Integrated Security=false;MultipleActiveResultSets=true;Connection Timeout=3600;",
    "CommandTimeoutSeconds": 3600,
    "LogDirectory": "C:\\RapidZ\\Logs"
  }
}
```

**Configuration Parameters:**
- `Server`: SQL Server instance name or IP address
- `Database`: Target database name
- `User Id`: Database username
- `Password`: Database password
- `CommandTimeoutSeconds`: Query timeout in seconds (3600 = 1 hour)
- `LogDirectory`: Directory for application logs

### 2. Export Configuration

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
      "OutputDirectory": "C:\\RapidZ\\EXPORT_Excel"
    },
    "Logging": {
      "OperationLabel": "Excel Export Generation",
      "LogFilePrefix": "Export_Log",
      "LogFileExtension": ".txt"
    }
  }
}
```

### 3. Import Configuration

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
      "OutputDirectory": "C:\\RapidZ\\IMPORT_Excel",
      "FileSuffix": "IMP"
    },
    "Logging": {
      "OperationLabel": "Excel Import Generation",
      "LogFilePrefix": "Import_Log",
      "LogFileExtension": ".txt"
    }
  }
}
```

### 4. Directory Setup

Create required directories:

#### Windows
```cmd
mkdir "C:\RapidZ\Logs"
mkdir "C:\RapidZ\EXPORT_Excel"
mkdir "C:\RapidZ\IMPORT_Excel"
```

#### Linux/macOS
```bash
mkdir -p ~/RapidZ/Logs
mkdir -p ~/RapidZ/EXPORT_Excel
mkdir -p ~/RapidZ/IMPORT_Excel
```

### 5. Permissions Setup

Ensure the application has write permissions to:
- Log directory
- Export output directory
- Import output directory

#### Windows
```cmd
icacls "C:\RapidZ\Logs" /grant Users:F
icacls "C:\RapidZ\EXPORT_Excel" /grant Users:F
icacls "C:\RapidZ\IMPORT_Excel" /grant Users:F
```

## First Run

### 1. Initial Launch

#### Windows
```cmd
cd C:\RapidZ
RapidZ.exe
```

#### Linux/macOS
```bash
cd ~/RapidZ
./RapidZ
```

### 2. Connection Test

The application will automatically test the database connection on startup:
- **Success**: Green indicator in the status bar
- **Failure**: Red indicator with error message

### 3. Initial Configuration Validation

The application will validate:
- Database connectivity
- Required database objects
- File system permissions
- Configuration file integrity

## Verification

### 1. Database Connection Test

1. Launch RapidZ
2. Check the status indicator in the footer
3. Verify connection status in the monitoring panel

### 2. Export Test

1. Switch to Export mode
2. Set minimal filter parameters:
   - From Month: Current month
   - To Month: Current month
   - Single port, HS code, product
3. Click "Start Export"
4. Verify Excel file generation in output directory

### 3. Import Test

1. Switch to Import mode
2. Set minimal filter parameters
3. Click "Start Import"
4. Verify operation completion

### 4. Log Verification

Check log files in the configured log directory:
- Application startup logs
- Database connection logs
- Operation execution logs

## Troubleshooting

### Common Installation Issues

#### 1. .NET Runtime Not Found
**Error**: "The framework 'Microsoft.NETCore.App', version '8.0.0' was not found"

**Solution**:
- Install .NET 8.0 Runtime
- Verify installation with `dotnet --version`
- Restart terminal/command prompt

#### 2. Database Connection Failure
**Error**: "Cannot connect to SQL Server"

**Solutions**:
- Verify server name/IP address
- Check database credentials
- Ensure SQL Server is running
- Verify network connectivity
- Check firewall settings

#### 3. Permission Denied Errors
**Error**: "Access to the path is denied"

**Solutions**:
- Run application as administrator (Windows)
- Check directory permissions
- Verify user has write access to output directories

#### 4. Missing Database Objects
**Error**: "Invalid object name 'EXPDATA'"

**Solutions**:
- Verify database objects exist
- Check user permissions on database objects
- Ensure correct database is specified in connection string

#### 5. Configuration File Errors
**Error**: "Configuration file not found" or "Invalid JSON"

**Solutions**:
- Verify configuration files exist in Config directory
- Validate JSON syntax
- Check file permissions
- Restore default configuration files

### Performance Issues

#### Slow Database Queries
- Check database indexes on filtered columns
- Review query execution plans
- Consider database maintenance

#### Memory Issues
- Increase system RAM
- Reduce batch sizes
- Monitor memory usage during operations

#### File I/O Issues
- Check disk space
- Verify antivirus exclusions
- Use SSD storage for better performance

### Getting Help

1. **Check Logs**: Review application logs for detailed error information
2. **Documentation**: Consult the main README.md file
3. **Database Admin**: Contact database administrator for database-related issues
4. **System Admin**: Contact system administrator for infrastructure issues

### Log File Locations

- **Application Logs**: `{LogDirectory}/RapidZ_{timestamp}.log`
- **Export Logs**: `{LogDirectory}/Export_Log_{timestamp}.txt`
- **Import Logs**: `{LogDirectory}/Import_Log_{timestamp}.txt`

---

**Next Steps**: After successful installation, refer to the User Guide for detailed usage instructions.