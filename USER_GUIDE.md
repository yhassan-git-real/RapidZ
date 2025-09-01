# RapidZ User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [Application Overview](#application-overview)
3. [Database Connection](#database-connection)
4. [Export Operations](#export-operations)
5. [Import Operations](#import-operations)
6. [Understanding Results](#understanding-results)
7. [Monitoring and Status](#monitoring-and-status)
8. [File Management](#file-management)
9. [Troubleshooting](#troubleshooting)
10. [Tips and Best Practices](#tips-and-best-practices)

## Getting Started

### First Launch

1. **Start the Application**
   - Double-click the RapidZ executable or launch from Start Menu
   - The application will open with the main dashboard

2. **Initial Setup Check**
   - Verify database connection status (top-right corner)
   - Check that configuration files are properly loaded
   - Ensure output directories exist and are accessible

3. **Interface Overview**
   - **Left Panel**: Navigation between Export and Import operations
   - **Center Panel**: Main operation interface with input fields
   - **Right Panel**: Status, monitoring, and results information
   - **Bottom Panel**: Progress indicators and operation logs

## Application Overview

### Main Interface Components

#### Navigation Panel
- **Export Tab**: Switch to export operations
- **Import Tab**: Switch to import operations
- **Settings**: Access configuration options

#### Operation Panel
- **Date Range**: Select from/to months for data extraction
- **Filter Options**: Configure various data filters
- **Database Objects**: Select views and stored procedures
- **Action Buttons**: Start, cancel, and manage operations

#### Status Panel
- **Database Connection**: Real-time connection status
- **Operation Progress**: Current operation status and progress
- **Results Summary**: Completed operations and file counts
- **System Monitoring**: Performance and resource usage

## Database Connection

### Connection Status

The database connection status is displayed in the top-right corner:

- **🟢 Connected**: Database is accessible and responsive
- **🟡 Connecting**: Attempting to establish connection
- **🔴 Disconnected**: No database connection available

### Connection Information

Hover over the connection status to see:
- Server name and database
- User account
- Last check time
- Response time

### Troubleshooting Connection Issues

1. **Check Network Connectivity**
   - Ensure network access to database server
   - Verify VPN connection if required

2. **Verify Credentials**
   - Check connection string in configuration
   - Ensure user account has necessary permissions

3. **Database Availability**
   - Confirm database server is running
   - Check for maintenance windows or outages

## Export Operations

### Step-by-Step Export Process

#### 1. Navigate to Export Tab
- Click the "Export" tab in the left navigation panel
- The export interface will load with default settings

#### 2. Configure Date Range
- **From Month**: Select starting month (format: YYYYMM)
- **To Month**: Select ending month (format: YYYYMM)
- Example: From "202401" to "202412" for full year 2024

#### 3. Set Up Filters

**Ports**
- Enter port codes separated by commas
- Example: `INMAA1, INJNP1, INCCU1`
- Leave empty to include all ports

**HS Codes**
- Enter HS codes separated by commas
- Example: `1234567890, 9876543210`
- Supports partial codes for broader matching

**Products**
- Enter product names or descriptions
- Example: `Steel, Iron Ore, Chemicals`
- Case-insensitive matching

**Exporters** (Export Operations Only)
- Enter exporter company names for export data extraction
- Example: `ABC Corp, XYZ Ltd`
- Supports partial name matching
- This parameter is specific to export operations and focuses on companies exporting goods

**IECs (Import Export Codes)**
- Enter IEC numbers separated by commas
- Example: `1234567890, 0987654321`

**Foreign Countries**
- Enter country names or codes
- Example: `USA, China, Germany`
- Use standard country names

**Foreign Names**
- Enter foreign company or entity names
- Example: `Foreign Corp, International Ltd`

#### 4. Select Export Database Objects

**Export View Selection**
- Choose from export-specific database views:
  - `EXPDATA` (Default) - Main export data view with sb_DATE ordering
  - `EXPDATA_DETAILED` - Detailed export data view
  - `EXPDATA_SUMMARY` - Summary export data view
- All export views use "sb_DATE" as the ordering column

**Export Stored Procedure Selection**
- Choose from export-specific stored procedures:
  - `ExportData_New1` (Default) - Main export data procedure
  - `ExportData_Detailed` - Detailed export processing
  - `ExportData_Summary` - Summary export processing
- Procedures are optimized for export data processing logic

#### 5. Optional: Custom Output Path
- Click "Browse" to select custom output directory
- Leave empty to use default configured path
- Ensure write permissions for selected directory

#### 6. Start Export Operation
- Click "Start Export" button
- Monitor progress in the status panel
- Operation can be cancelled at any time

### Export Results

After completion, you'll find:
- **Excel Files**: Generated in `F:\RapidZ\EXPORT_Excel` directory with EXP.xlsx suffix
- **Excel Formatting**: Export-specific formatting with date columns [3] and text columns [1,2,4]
- **Log Files**: Export-specific logs with "Export_Log" prefix
- **File Naming**: Files follow pattern `{core}_{monthRange}EXP.xlsx`
- **Summary Report**: Export operation statistics and file counts

## Import Operations

### Step-by-Step Import Process

#### 1. Navigate to Import Tab
- Click the "Import" tab in the left navigation panel
- The import interface will load with default settings

#### 2. Configure Date Range
- **From Month**: Select starting month (format: YYYYMM)
- **To Month**: Select ending month (format: YYYYMM)
- Same format as export operations

#### 3. Set Up Filters

**Ports**
- Enter port codes for import data
- Example: `INJNP1, INCCU1, INMAA1`

**HS Codes**
- Enter HS codes for specific products
- Same format as export operations

**Products**
- Enter imported product names
- Example: `Electronics, Machinery, Textiles`

**Importers** (Import Operations Only)
- Enter importer company names for import data extraction
- Example: `Import Corp, Trade Ltd`
- Supports partial name matching
- This parameter is specific to import operations and focuses on companies importing goods

**IECs (Import Export Codes)**
- Enter relevant IEC numbers
- Format same as export operations

**Foreign Countries**
- Enter origin countries for imports
- Example: `China, USA, Japan`

**Foreign Names**
- Enter foreign supplier names
- Example: `Supplier Inc, Manufacturer Ltd`

#### 4. Select Import Database Objects

**Import View Selection**
- Choose from import-specific database views:
  - `IMPDATA` (Default) - Main import data view with DATE ordering
  - `IMPDATA_DETAILED` - Detailed import data view
  - `IMPDATA_SUMMARY` - Summary import data view
- All import views use "DATE" as the ordering column

**Import Stored Procedure Selection**
- Choose from import-specific stored procedures:
  - `ImportJNPTData_New1` (Default) - Main import data procedure
  - `ImportJNPTData_Detailed` - Detailed import processing
  - `ImportJNPTData_Summary` - Summary import processing
- Procedures are optimized for import data processing and different ports/data types

#### 5. Optional: Custom Output Path
- Select custom directory for import files
- Default path used if not specified

#### 6. Start Import Operation
- Click "Start Import" button
- Monitor real-time progress
- Cancel if needed using "Cancel" button

### Import Results

Generated files include:
- **Excel Workbooks**: Processed import data in `F:\RapidZ\IMPORT_Excel` directory with IMP.xlsx suffix
- **Excel Formatting**: Import-specific formatting with date columns [2] and text columns [1,3,4]
- **Data Sheets**: Organized by categories with import-specific structure
- **File Naming**: Files follow pattern `{core}_{monthRange}IMP.xlsx`
- **Log Files**: Import-specific logs with "Import_Log" prefix
- **Summary Reports**: Import operation statistics

## Understanding Results

### File Naming Convention

**Export Files**
```
{core}_{monthRange}EXP.xlsx
```
Example: `Export_INMAA1_202401-202412_Steel_ABCCorp_EXP.xlsx`
- Generated using Export_FileNameHelper
- Always ends with "EXP.xlsx" suffix
- Includes exporter-focused parameters

**Import Files**
```
{core}_{monthRange}IMP.xlsx
```
Example: `Import_INJNP1_202401-202412_Electronics_ImportCorp_IMP.xlsx`
- Generated using Import_FileNameHelper
- Always ends with "IMP.xlsx" suffix (configurable via FileSuffix setting)
- Includes importer-focused parameters

**Key Differences**:
- Export files focus on **Exporters** and use **EXP.xlsx** suffix
- Import files focus on **Importers** and use **IMP.xlsx** suffix
- Different helper classes ensure consistent naming patterns

## Key Differences Between Export and Import Operations

### Database Architecture

#### Export Operations
- **Views**: EXPDATA, EXPDATA_DETAILED, EXPDATA_SUMMARY
- **Stored Procedures**: ExportData_New1, ExportData_Detailed, ExportData_Summary
- **Ordering Column**: "sb_DATE" for all export views
- **Focus**: Export trade data and exporter companies

#### Import Operations
- **Views**: IMPDATA, IMPDATA_DETAILED, IMPDATA_SUMMARY
- **Stored Procedures**: ImportJNPTData_New1, ImportJNPTData_Detailed, ImportJNPTData_Summary
- **Ordering Column**: "DATE" for all import views
- **Focus**: Import trade data and importer companies

### Excel Formatting Differences

#### Export Excel Formatting
- **Date Columns**: Column 3 receives date-specific formatting
- **Text Columns**: Columns 1, 2, and 4 receive text-specific formatting
- **Configuration**: Uses ExportExcelFormatSettings.json

#### Import Excel Formatting
- **Date Columns**: Column 2 receives date-specific formatting
- **Text Columns**: Columns 1, 3, and 4 receive text-specific formatting
- **Configuration**: Uses ImportExcelFormatSettings.json

### File Management

#### Export Files
- **Output Directory**: `F:\RapidZ\EXPORT_Excel`
- **File Suffix**: Always "EXP.xlsx"
- **Helper Class**: Export_FileNameHelper
- **Log Prefix**: "Export_Log"

#### Import Files
- **Output Directory**: `F:\RapidZ\IMPORT_Excel`
- **File Suffix**: "IMP.xlsx" (configurable via FileSuffix setting)
- **Helper Class**: Import_FileNameHelper
- **Log Prefix**: "Import_Log"

### Parameter Focus

#### Export Parameters
- **Primary Focus**: Exporter companies and export-related data
- **Company Field**: "Exporters" - companies sending goods out of the country
- **Data Perspective**: Outbound trade transactions

#### Import Parameters
- **Primary Focus**: Importer companies and import-related data
- **Company Field**: "Importers" - companies bringing goods into the country
- **Data Perspective**: Inbound trade transactions

### Excel File Structure

#### Worksheets (Common to Both Operations)
- **Data**: Main data sheet with filtered results
- **Summary**: Statistical summary and totals
- **Parameters**: Applied filters and settings
- **Metadata**: Operation details and timestamps

#### Data Columns
Common columns include:
- **Date**: Transaction date
- **Port**: Port of entry/exit
- **HS Code**: Harmonized System code
- **Product**: Product description
- **Company**: Importer/Exporter name
- **IEC**: Import Export Code
- **Country**: Origin/Destination country
- **Quantity**: Product quantity
- **Value**: Transaction value
- **Unit**: Measurement unit

### Log Files

**Log File Location**
- Default: `Logs` directory in application folder
- Separate logs for export and import operations

**Log File Content**
- Operation start/end times
- Applied filters and parameters
- Processing statistics
- Error messages and warnings
- Performance metrics

## Monitoring and Status

### Real-Time Monitoring

#### Operation Progress
- **Progress Bar**: Visual progress indicator
- **Current Status**: Text description of current activity
- **Elapsed Time**: Time since operation started
- **Estimated Remaining**: Calculated remaining time

#### Performance Metrics
- **Records Processed**: Number of database records processed
- **Files Generated**: Count of Excel files created
- **Processing Rate**: Records per second
- **Memory Usage**: Current application memory consumption

#### Database Monitoring
- **Connection Status**: Real-time connection health
- **Query Performance**: Database response times
- **Active Connections**: Number of active database connections

### Status Indicators

**Operation Status**
- 🟢 **Running**: Operation in progress
- 🟡 **Paused**: Operation temporarily paused
- 🔴 **Stopped**: Operation cancelled or failed
- ✅ **Completed**: Operation finished successfully

**System Status**
- 🟢 **Healthy**: All systems operating normally
- 🟡 **Warning**: Minor issues detected
- 🔴 **Error**: Critical issues requiring attention

## File Management

### Output Directory Structure

```
F:\RapidZ\
├── EXPORT_Excel/          # Export operations output
│   ├── Export_*.xlsx      # Export files with EXP.xlsx suffix
│   └── Archive/
├── IMPORT_Excel/          # Import operations output
│   ├── Import_*.xlsx      # Import files with IMP.xlsx suffix
│   └── Archive/
└── Logs/
    ├── Export_Log_*.log   # Export operation logs
    └── Import_Log_*.log   # Import operation logs
```

**Key Directory Differences**:
- **Export Files**: Stored in `EXPORT_Excel` directory
- **Import Files**: Stored in `IMPORT_Excel` directory
- **Log Separation**: Export logs use "Export_Log" prefix, Import logs use "Import_Log" prefix
- **File Naming**: Export files end with "EXP.xlsx", Import files end with "IMP.xlsx"

### File Management Features

#### Automatic Organization
- Files organized by year and month
- Separate folders for export and import
- Automatic archive of old files

#### File Cleanup
- **Manual Cleanup**: Use "Clear Files" button
- **Automatic Cleanup**: Configurable retention policies
- **Archive Options**: Move old files to archive folders

#### File Access
- **Open Folder**: Quick access to output directories
- **Open File**: Direct file opening from results panel
- **Copy Path**: Copy file paths to clipboard

### Storage Considerations

**Disk Space**
- Monitor available disk space
- Large operations can generate significant data
- Configure cleanup policies to manage storage

**File Permissions**
- Ensure write access to output directories
- Check antivirus exclusions if needed
- Verify network drive permissions for shared storage

## Troubleshooting

### Common Issues and Solutions

#### "Database Connection Failed"
**Symptoms**: Red connection indicator, unable to start operations

**Solutions**:
1. Check network connectivity
2. Verify database server status
3. Validate connection string in configuration
4. Ensure user permissions are correct
5. Check firewall settings

#### "No Data Found"
**Symptoms**: Operation completes but generates empty files

**Solutions**:
1. Verify date range includes data
2. Check filter criteria - may be too restrictive
3. Confirm database views contain expected data
4. Review stored procedure logic
5. Check database permissions for data access

#### "Operation Cancelled"
**Symptoms**: Operation stops unexpectedly

**Solutions**:
1. Check for user cancellation
2. Verify sufficient disk space
3. Monitor system resources (memory, CPU)
4. Check for database timeouts
5. Review error logs for specific issues

#### "File Access Denied"
**Symptoms**: Cannot write to output directory

**Solutions**:
1. Check directory permissions
2. Ensure directory exists
3. Verify antivirus is not blocking access
4. Close any open Excel files in output directory
5. Run application as administrator if needed

#### "Memory Issues"
**Symptoms**: Application becomes slow or unresponsive

**Solutions**:
1. Reduce date range for large operations
2. Use more specific filters to limit data
3. Close other applications to free memory
4. Restart application periodically
5. Consider processing data in smaller batches

### Performance Optimization

#### For Large Data Sets
1. **Use Specific Filters**: Narrow down data selection
2. **Smaller Date Ranges**: Process months individually
3. **Off-Peak Hours**: Run during low database usage
4. **Batch Processing**: Split large operations into smaller ones

#### For Better Performance
1. **Close Unnecessary Applications**: Free system resources
2. **Use SSD Storage**: Faster file I/O operations
3. **Adequate RAM**: Ensure sufficient memory available
4. **Network Optimization**: Use wired connection for database access

### Getting Help

#### Log Analysis
1. Check application logs in the Logs directory
2. Look for error messages and timestamps
3. Note any patterns in failures
4. Include relevant log excerpts when reporting issues

#### Error Reporting
When reporting issues, include:
- Error message text
- Steps to reproduce
- System specifications
- Database configuration (without sensitive data)
- Relevant log file excerpts

## Tips and Best Practices

### Efficient Operations

#### Planning Your Queries
1. **Start Small**: Test with limited date ranges first
2. **Use Specific Filters**: Avoid overly broad queries
3. **Monitor Resources**: Watch memory and disk usage
4. **Schedule Large Operations**: Run during off-peak hours

#### Data Management
1. **Regular Cleanup**: Remove old files periodically
2. **Archive Important Data**: Keep copies of critical exports
3. **Backup Configuration**: Save configuration files
4. **Document Procedures**: Keep notes on successful filter combinations

### Quality Assurance

#### Verify Results
1. **Check File Counts**: Ensure expected number of files generated
2. **Spot Check Data**: Review sample records for accuracy
3. **Compare Totals**: Verify summary statistics make sense
4. **Cross-Reference**: Compare with previous similar operations

#### Data Validation
1. **Date Ranges**: Ensure dates are within expected bounds
2. **Filter Logic**: Verify filters produce expected results
3. **Database Objects**: Use appropriate views and procedures
4. **Output Format**: Check Excel files open correctly

### Security Considerations

#### Data Protection
1. **Access Control**: Limit access to sensitive data
2. **File Security**: Protect generated files appropriately
3. **Network Security**: Use secure database connections
4. **Audit Trail**: Maintain logs of data access

#### Best Practices
1. **Regular Updates**: Keep application updated
2. **Secure Configuration**: Protect configuration files
3. **User Training**: Ensure users understand security policies
4. **Incident Response**: Have procedures for security issues

### Maintenance

#### Regular Tasks
1. **Update Database Objects**: Keep views and procedures current
2. **Review Configurations**: Update settings as needed
3. **Monitor Performance**: Track operation times and success rates
4. **Clean Up Files**: Manage disk space usage

#### Periodic Reviews
1. **Audit Usage**: Review who is using the system
2. **Performance Analysis**: Identify optimization opportunities
3. **Configuration Review**: Ensure settings remain appropriate
4. **Training Updates**: Keep users informed of new features

---

## Quick Reference

### Keyboard Shortcuts
- **Ctrl + E**: Switch to Export tab
- **Ctrl + I**: Switch to Import tab
- **Ctrl + S**: Start current operation
- **Ctrl + C**: Cancel current operation
- **F5**: Refresh database connection
- **Ctrl + L**: Open log directory
- **Ctrl + O**: Open output directory

### Common Filter Examples

**Export All Data for Specific Port**
- Ports: `INMAA1`
- All other fields: empty

**Import Electronics from China**
- Products: `Electronics, Electronic`
- Foreign Countries: `China`
- All other fields: empty

**Specific Company Data**
- Exporters/Importers: `Company Name`
- IECs: `1234567890`
- All other fields: empty

**Monthly Data Review**
- From Month: `202401`
- To Month: `202401`
- All other fields: empty

---

*For technical support or additional questions, refer to the troubleshooting section or contact your system administrator.*