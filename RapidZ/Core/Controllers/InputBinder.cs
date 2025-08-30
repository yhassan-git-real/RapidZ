using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using RapidZ.Core.Parameters.Export;
using RapidZ.Core.Parameters.Import;
using RapidZ.Core.Models;
using RapidZ.Views.Controls;

namespace RapidZ.Core.Controllers
{
    /// <summary>
    /// Utility class for binding UI controls to input DTOs
    /// Adapted for Avalonia UI controls
    /// </summary>
    public static class InputBinder
    {
        /// <summary>
        /// Creates ExportInputs from Avalonia UI controls
        /// </summary>
        /// <param name="fromMonthPicker">From month picker control</param>
        /// <param name="toMonthPicker">To month picker control</param>
        /// <param name="portsTextBox">Ports text box control</param>
        /// <param name="hsCodesTextBox">HS codes text box control</param>
        /// <param name="productsTextBox">Products text box control</param>
        /// <param name="exportersTextBox">Exporters text box control</param>
        /// <param name="foreignCountriesTextBox">Foreign countries text box control</param>
        /// <param name="foreignNamesTextBox">Foreign names text box control</param>
        /// <param name="iecsTextBox">IECs text box control</param>
        /// <returns>ExportInputs DTO populated from UI controls</returns>
        public static ExportInputs GetExportInputs(YearMonthPicker fromMonthPicker, YearMonthPicker toMonthPicker)
        {
            return new ExportInputs(
                FromMonth: fromMonthPicker.Value ?? string.Empty,
                ToMonth: toMonthPicker.Value ?? string.Empty,
                Ports: new List<string>(),
                HSCodes: new List<string>(),
                Products: new List<string>(),
                Exporters: new List<string>(),
                IECs: new List<string>(),
                ForeignCountries: new List<string>(),
                ForeignNames: new List<string>()
            );
        }

        /// <summary>
        /// Creates ImportInputs from Avalonia UI controls
        /// </summary>
        /// <param name="fromMonthPicker">From month picker control</param>
        /// <param name="toMonthPicker">To month picker control</param>
        /// <param name="portsTextBox">Ports text box control</param>
        /// <param name="hsCodesTextBox">HS codes text box control</param>
        /// <param name="productsTextBox">Products text box control</param>
        /// <param name="importersTextBox">Importers text box control</param>
        /// <param name="foreignCountriesTextBox">Foreign countries text box control</param>
        /// <param name="foreignNamesTextBox">Foreign names text box control</param>
        /// <param name="iecsTextBox">IECs text box control</param>
        /// <returns>ImportInputs DTO populated from UI controls</returns>
        public static ImportInputs GetImportInputs(YearMonthPicker fromMonthPicker, YearMonthPicker toMonthPicker)
        {
            return new ImportInputs(
                FromMonth: fromMonthPicker.Value ?? string.Empty,
                ToMonth: toMonthPicker.Value ?? string.Empty,
                Ports: new List<string>(),
                HSCodes: new List<string>(),
                Products: new List<string>(),
                Importers: new List<string>(),
                IECs: new List<string>(),
                ForeignCountries: new List<string>(),
                ForeignNames: new List<string>()
            );
        }

        /// <summary>
        /// Parses comma-separated values from text box text
        /// </summary>
        /// <param name="text">The text to parse</param>
        /// <returns>List of trimmed, non-empty values</returns>
        private static List<string> ParseTextBoxValues(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string> { "" }; // Return empty string as default

            return text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .ToList();
        }
    }
}