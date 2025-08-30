using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RapidZ.Core.Helpers
{
    public static class ImportParameterHelper
    {
        // Import parameter constants
        public const string FROM_MONTH = "fromMonth";
        public const string TO_MONTH = "toMonth";
        public const string HS_CODE = "hsCode";
        public const string PRODUCT = "product";
        public const string IEC = "iec";
        public const string IMPORTER = "importer";
        public const string COUNTRY = "country";
        public const string NAME = "name";
        public const string PORT = "port";

        // Stored procedure parameter constants
        public const string SP_FROM_MONTH = "@fromMonth";
        public const string SP_TO_MONTH = "@ToMonth";
        public const string SP_HS_CODE = "@hs";
        public const string SP_PRODUCT = "@prod";
        public const string SP_IEC = "@Iec";
        public const string SP_IMPORTER = "@ImpCmp";
        public const string SP_COUNTRY = "@forcount";
        public const string SP_NAME = "@forname";
        public const string SP_PORT = "@port";

        /// <summary>
        /// Validates if the date format is correct (YYYYMM)
        /// </summary>
        public static bool IsValidDateFormat(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString.Length != 6)
                return false;

            return DateTime.TryParseExact(dateString + "01", "yyyyMMdd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Validates if the date range is valid (fromMonth <= toMonth)
        /// </summary>
        public static bool IsValidDateRange(string fromMonth, string toMonth)
        {
            if (!IsValidDateFormat(fromMonth) || !IsValidDateFormat(toMonth))
                return false;

            return string.Compare(fromMonth, toMonth, StringComparison.Ordinal) <= 0;
        }

        /// <summary>
        /// Normalizes parameter value (trims and handles empty strings)
        /// </summary>
        public static string NormalizeParameter(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// Parses a comma-separated filter list and returns normalized values
        /// </summary>
        public static List<string> ParseFilterList(string? filterValue)
        {
            if (string.IsNullOrWhiteSpace(filterValue))
                return new List<string>();

            return filterValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrEmpty(s))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToList();
        }

        /// <summary>
        /// Creates a dictionary of import parameters for easy access
        /// </summary>
        public static Dictionary<string, string> CreateImportParameterSet(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { FROM_MONTH, NormalizeParameter(fromMonth) },
                { TO_MONTH, NormalizeParameter(toMonth) },
                { HS_CODE, NormalizeParameter(hsCode) },
                { PRODUCT, NormalizeParameter(product) },
                { IEC, NormalizeParameter(iec) },
                { IMPORTER, NormalizeParameter(importer) },
                { COUNTRY, NormalizeParameter(country) },
                { NAME, NormalizeParameter(name) },
                { PORT, NormalizeParameter(port) }
            };
        }

        /// <summary>
        /// Validates all import parameters
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateImportParameters(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port)
        {
            var errors = new List<string>();

            // Validate required date parameters
            if (string.IsNullOrWhiteSpace(fromMonth))
                errors.Add("From Month is required");
            else if (!IsValidDateFormat(fromMonth))
                errors.Add("From Month must be in YYYYMM format");

            if (string.IsNullOrWhiteSpace(toMonth))
                errors.Add("To Month is required");
            else if (!IsValidDateFormat(toMonth))
                errors.Add("To Month must be in YYYYMM format");

            // Validate date range
            if (!string.IsNullOrWhiteSpace(fromMonth) && !string.IsNullOrWhiteSpace(toMonth) &&
                IsValidDateFormat(fromMonth) && IsValidDateFormat(toMonth))
            {
                if (!IsValidDateRange(fromMonth, toMonth))
                    errors.Add("From Month must be less than or equal to To Month");
            }

            // Validate parameter lengths (prevent SQL injection and ensure reasonable limits)
            var parameters = new Dictionary<string, string>
            {
                { "HS Code", hsCode },
                { "Product", product },
                { "IEC", iec },
                { "Importer", importer },
                { "Country", country },
                { "Name", name },
                { "Port", port }
            };

            foreach (var param in parameters)
            {
                if (!string.IsNullOrEmpty(param.Value) && param.Value.Length > 255)
                    errors.Add($"{param.Key} cannot exceed 255 characters");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Formats month string for display (YYYYMM -> MMM YYYY)
        /// </summary>
        public static string FormatMonthForDisplay(string monthString)
        {
            if (!IsValidDateFormat(monthString))
                return monthString;

            if (DateTime.TryParseExact(monthString + "01", "yyyyMMdd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            }

            return monthString;
        }

        /// <summary>
        /// Creates a summary string of non-empty parameters for logging
        /// </summary>
        public static string CreateParameterSummary(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port)
        {
            var summary = new List<string>();

            if (!string.IsNullOrWhiteSpace(fromMonth) && !string.IsNullOrWhiteSpace(toMonth))
                summary.Add($"Period: {FormatMonthForDisplay(fromMonth)} - {FormatMonthForDisplay(toMonth)}");

            if (!string.IsNullOrWhiteSpace(hsCode))
                summary.Add($"HS Code: {hsCode}");

            if (!string.IsNullOrWhiteSpace(product))
                summary.Add($"Product: {product}");

            if (!string.IsNullOrWhiteSpace(iec))
                summary.Add($"IEC: {iec}");

            if (!string.IsNullOrWhiteSpace(importer))
                summary.Add($"Importer: {importer}");

            if (!string.IsNullOrWhiteSpace(country))
                summary.Add($"Country: {country}");

            if (!string.IsNullOrWhiteSpace(name))
                summary.Add($"Name: {name}");

            if (!string.IsNullOrWhiteSpace(port))
                summary.Add($"Port: {port}");

            return string.Join(", ", summary);
        }
    }
}