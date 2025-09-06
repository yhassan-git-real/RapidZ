using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using ReactiveUI;
using RapidZ.Views.Models;
using RapidZ.Core.Models;
using RapidZ.Core.Services;
using RapidZ.Features.Common.ViewModels;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for properties and backing fields
/// </summary>
public partial class MainViewModel
{
    // Basic state backing fields
    private bool _isBusy;
    private double _progressPercentage = 0;
    private bool _canCancel = false;
    private ConnectionInfo _connectionInfo = new();
    private string _currentMode = "Export"; // Default to Export mode
    private SystemStatus _systemStatus = SystemStatus.Idle;
    
    // Execution summary properties
    private ExecutionSummary _lastExecution = ExecutionSummary.Empty;
    private bool _showExecutionSummary = false;
    private IDisposable? _logCheckSubscription;
    
    // Date properties
    private int _fromYear;
    private int _fromMonth;
    private int _toYear;
    private int _toMonth;
    
    // Database object selection properties
    private DbObjectOption? _selectedView;
    private DbObjectOption? _selectedStoredProcedure;
    private ObservableCollection<DbObjectOption>? _availableViews;
    private ObservableCollection<DbObjectOption>? _availableStoredProcedures;
    
    // Database object validation properties
    private bool _isViewValid = true;
    private bool _isStoredProcedureValid = true;
    private string _viewValidationMessage = string.Empty;
    private string _storedProcedureValidationMessage = string.Empty;
    
    // Mandatory field validation properties
    private bool _areMandatoryFieldsValid = false;
    private string _mandatoryFieldsValidationMessage = string.Empty;
    
    // Input parameter validation properties
    private bool _areInputParametersValid = false;
    private string _inputParametersValidationMessage = string.Empty;

    // Export data filter for binding - critical for UI
    public ExportDataFilter ExportDataFilter { get; set; } = new();
}
