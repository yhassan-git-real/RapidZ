using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using RapidZ.Views.Models;
using RapidZ.Core.Models;
using RapidZ.Core.Controllers;
using RapidZ.Core.Services;
using RapidZ.Core.Helpers;
using RapidZ.Features.Common.ViewModels;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Import.Services;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for initialization logic
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Initialize default values
    /// </summary>
    private void InitializeDefaults()
    {
        var currentDate = DateTime.Now;
        _fromYear = currentDate.Year;
        _fromMonth = currentDate.Month;
        _toYear = currentDate.Year;
        _toMonth = currentDate.Month;
        
        ExportDataFilter = new ExportDataFilter
        {
            Mode = "Export", // Default mode is Export
            FromMonth = $"{_fromYear}{_fromMonth:D2}",
            ToMonth = $"{_toYear}{_toMonth:D2}",
            // Clear sample data while preserving placeholder functionality
            HSCode = string.Empty,
            Product = string.Empty,
            Exporter = string.Empty,
            IEC = string.Empty,
            ForeignParty = string.Empty,
            ForeignCountry = string.Empty,
            Port = string.Empty,
            CustomFilePath = string.Empty,
            UseCustomPath = false
        };
        
        // Set initial current mode
        _currentMode = "Export";
        
        // Initialize validation states
        _areInputParametersValid = false;
        _inputParametersValidationMessage = string.Empty;
        
        // Perform initial input parameter validation
        ValidateInputParameters();
        
        // Ensure we have a valid execution summary to avoid null reference exceptions
        _lastExecution = ExecutionSummary.Empty;
        
        // Initialize database selections based on current mode when the view is loaded
        Dispatcher.UIThread.Post(() => UpdateDatabaseSelectionsForMode(), DispatcherPriority.Background);
        
        // Start the periodic log checking if log parser is available
        StartLogMonitoring();
    }

    /// <summary>
    /// Initialize collections for dropdown binding
    /// </summary>
    private void InitializeCollections()
    {
        _availableViews = new ObservableCollection<DbObjectOption>();
        _availableStoredProcedures = new ObservableCollection<DbObjectOption>();
    }

    /// <summary>
    /// Load database objects from services when available
    /// </summary>
    private void LoadDataDirectlyFromConfiguration()
    {
        // Reset validation states
        IsViewValid = true;
        IsStoredProcedureValid = true;
        ViewValidationMessage = string.Empty;
        StoredProcedureValidationMessage = string.Empty;
        AreMandatoryFieldsValid = false;
        MandatoryFieldsValidationMessage = string.Empty;
        if (Services == null)
            return;

        try
        {
            // Clear existing collections
            AvailableViews?.Clear();
            AvailableStoredProcedures?.Clear();

            string currentMode = ExportDataFilter?.Mode ?? "Export";

            if (currentMode == "Export" && Services.ExportObjectValidationService != null)
            {
                var exportViews = Services.ExportObjectValidationService.GetAvailableViews();
                var exportProcs = Services.ExportObjectValidationService.GetAvailableStoredProcedures();
                
                AvailableViews = new ObservableCollection<DbObjectOption>(exportViews);
                AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(exportProcs);

                var defaultView = Services.ExportObjectValidationService.GetDefaultViewName();
                var defaultProc = Services.ExportObjectValidationService.GetDefaultStoredProcedureName();
                
                SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
            }
            else if (currentMode == "Import" && Services.ImportObjectValidationService != null)
            {
                var importViews = Services.ImportObjectValidationService.GetAvailableViews();
                var importProcs = Services.ImportObjectValidationService.GetAvailableStoredProcedures();
                
                AvailableViews = new ObservableCollection<DbObjectOption>(importViews);
                AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(importProcs);

                var defaultView = Services.ImportObjectValidationService.GetDefaultViewName();
                var defaultProc = Services.ImportObjectValidationService.GetDefaultStoredProcedureName();
                
                SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
            }
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to load database objects: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates database selections (views, stored procedures) based on the current application mode
    /// </summary>
    private void UpdateDatabaseSelectionsForMode()
    {
        if (Services == null)
        {
            
            Console.WriteLine("Services not initialized");
            return;
        }

        try
        {
            // Clear existing collections
            AvailableViews?.Clear();
            AvailableStoredProcedures?.Clear();

            // Use the current mode to determine which service to use
            string mode = CurrentMode ?? "Export"; // Default to Export if not set
            
            if (mode == "Export")
            {
                // Set database objects for Export mode
                if (Services.ExportObjectValidationService != null)
                {
                    var exportViews = Services.ExportObjectValidationService.GetAvailableViews();
                    var exportProcs = Services.ExportObjectValidationService.GetAvailableStoredProcedures();
                    
                    AvailableViews = new ObservableCollection<DbObjectOption>(exportViews);
                    AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(exportProcs);
    
                    var defaultView = Services.ExportObjectValidationService.GetDefaultViewName();
                    var defaultProc = Services.ExportObjectValidationService.GetDefaultStoredProcedureName();
                    
                    SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                    SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
                    
                    // Export mode: Database objects loaded successfully
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects();
                }
                else
                {
                    // Export validation service not available
                    Console.WriteLine("Export validation service not available");
                }
            }
            else if (mode == "Import")
            {
                // Set database objects for Import mode
                if (Services.ImportObjectValidationService != null)
                {
                    var importViews = Services.ImportObjectValidationService.GetAvailableViews();
                    var importProcs = Services.ImportObjectValidationService.GetAvailableStoredProcedures();
                    
                    AvailableViews = new ObservableCollection<DbObjectOption>(importViews);
                    AvailableStoredProcedures = new ObservableCollection<DbObjectOption>(importProcs);
    
                    var defaultView = Services.ImportObjectValidationService.GetDefaultViewName();
                    var defaultProc = Services.ImportObjectValidationService.GetDefaultStoredProcedureName();
                    
                    SelectedView = AvailableViews.FirstOrDefault(v => v.Name == defaultView);
                    SelectedStoredProcedure = AvailableStoredProcedures.FirstOrDefault(p => p.Name == defaultProc);
                    
                    // Import mode: Database objects loaded successfully
                    // Validate selected database objects
                    ValidateSelectedDatabaseObjects();
                }
                else
                {
                    // Import validation service not available
                    Console.WriteLine("Import validation service not available");
                }
            }
            
            // Update ExporterLabelText
            this.RaisePropertyChanged(nameof(ExporterLabelText));
            this.RaisePropertyChanged(nameof(FileLocationLabelText));
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Failed to update database selections: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles busy state changes from services
    /// </summary>
    private void OnBusyStateChanged(bool isBusy)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = isBusy;
            CanCancel = isBusy;
            
            // If becoming idle (operation finished), set status to Idle
            // Specific success/failure states are set elsewhere based on results
            if (!isBusy)
            {
                // This will be overridden by success/failure handlers if needed
                SystemStatus = SystemStatus.Idle;
            }
        });
    }
}
