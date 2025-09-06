using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace RapidZ.Views.ViewModels;

/// <summary>
/// Partial class for validation logic
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Validates the selected view and stored procedure against the database and mandatory field requirements
    /// </summary>
    private void ValidateSelectedDatabaseObjects()
    {
        // First, validate mandatory field selection (both fields must be selected)
        ValidateMandatoryFields();
        
        // If services or DatabaseObjectValidator are not available, or objects are not selected, can't validate existence
        if (Services?.DatabaseObjectValidator == null || SelectedView == null || SelectedStoredProcedure == null)
        {
            // Set validation to false if objects are not selected
            if (SelectedView == null)
            {
                IsViewValid = false;
                ViewValidationMessage = "View selection is required";
            }
            
            if (SelectedStoredProcedure == null)
            {
                IsStoredProcedureValid = false;
                StoredProcedureValidationMessage = "Stored procedure selection is required";
            }
            
            return;
        }

        string viewName = SelectedView.Name;
        string procName = SelectedStoredProcedure.Name;

        // Validate the view exists in database
        IsViewValid = Services.DatabaseObjectValidator.ViewExists(viewName);
        ViewValidationMessage = IsViewValid ? string.Empty : 
            "Object does not exist in database";

        // Validate the stored procedure exists in database
        IsStoredProcedureValid = Services.DatabaseObjectValidator.StoredProcedureExists(procName);
        StoredProcedureValidationMessage = IsStoredProcedureValid ? string.Empty : 
            "Object does not exist in database";
    }
    
    /// <summary>
    /// Method to validate custom path settings
    /// </summary>
    private async void ValidateCustomPath()
    {
        // Check if path is provided but checkbox not selected - highlight checkbox
        if (!string.IsNullOrWhiteSpace(ExportDataFilter.CustomFilePath) && !ExportDataFilter.UseCustomPath)
        {
            ShouldHighlightCheckbox = true;
            IsCustomPathValid = false;
            CustomPathValidationMessage = "Please check 'Use Custom Path' to use the specified custom file path.";
            return;
        }
        else
        {
            ShouldHighlightCheckbox = false;
        }
        
        if (ExportDataFilter.UseCustomPath)
        {
            if (string.IsNullOrWhiteSpace(ExportDataFilter.CustomFilePath))
            {
                IsCustomPathValid = false;
                CustomPathValidationMessage = "Custom file path is required when 'Use Custom Path' is checked.";
                return;
            }
            
            // Use PathValidationService to validate the directory
            if (_pathValidationService != null)
            {
                var validationResult = await _pathValidationService.ValidateDirectoryPathAsync(ExportDataFilter.CustomFilePath);
                IsCustomPathValid = validationResult.IsValid;
                CustomPathValidationMessage = validationResult.ErrorMessage ?? string.Empty;
            }
        }
        else
        {
            IsCustomPathValid = true;
            CustomPathValidationMessage = string.Empty;
        }
    }
    
    /// <summary>
    /// Validates that both mandatory fields (View and Stored Procedure) are selected
    /// </summary>
    private void ValidateMandatoryFields()
    {
        bool viewSelected = SelectedView != null;
        bool storedProcSelected = SelectedStoredProcedure != null;
        
        AreMandatoryFieldsValid = viewSelected && storedProcSelected;
        
        if (!AreMandatoryFieldsValid)
        {
            if (!viewSelected && !storedProcSelected)
            {
                MandatoryFieldsValidationMessage = "Both View and Stored Procedure must be selected";
            }
            else if (!viewSelected)
            {
                MandatoryFieldsValidationMessage = "View selection is required";
            }
            else if (!storedProcSelected)
            {
                MandatoryFieldsValidationMessage = "Stored Procedure selection is required";
            }
        }
        else
        {
            MandatoryFieldsValidationMessage = string.Empty;
        }
    }
    
    /// <summary>
    /// Validates that at least one input parameter field contains a value (excluding '%' wildcard)
    /// </summary>
    private void ValidateInputParameters()
    {
        // Check if at least one parameter field has a meaningful value (not empty, null, or '%')
        bool hasValidParameter = HasValidParameterValue(ExportDataFilter.HSCode) ||
                                HasValidParameterValue(ExportDataFilter.Product) ||
                                HasValidParameterValue(ExportDataFilter.Exporter) ||
                                HasValidParameterValue(ExportDataFilter.IEC) ||
                                HasValidParameterValue(ExportDataFilter.ForeignParty) ||
                                HasValidParameterValue(ExportDataFilter.ForeignCountry) ||
                                HasValidParameterValue(ExportDataFilter.Port);
        
        AreInputParametersValid = hasValidParameter;
        
        if (!AreInputParametersValid)
        {
            InputParametersValidationMessage = "At least one input parameter field must contain a value";
        }
        else
        {
            InputParametersValidationMessage = string.Empty;
        }
    }
    
    /// <summary>
    /// Checks if a parameter value is valid (not empty, null, whitespace, or '%' wildcard)
    /// </summary>
    /// <param name="value">The parameter value to check</param>
    /// <returns>True if the value is valid, false otherwise</returns>
    private bool HasValidParameterValue(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim() != "%";
    }
}
