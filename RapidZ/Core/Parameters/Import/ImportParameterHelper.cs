using System;
using System.Collections.Generic;
using System.Linq;
using RapidZ.Core.Helpers;

namespace RapidZ.Core.Parameters.Import
{
    /// <summary>
    /// Import parameter helper (independent from export logic).
    /// </summary>
    public static class ImportParameterHelper
    {
        // Use the constants from BaseParameterHelper
        public const string WILDCARD = BaseParameterHelper.WILDCARD;
        public const string DEFAULT_DATE_FORMAT = DateHelper.DEFAULT_DATE_FORMAT;
        // Use DateHelper constants for date validation
        public const int MIN_DATE_VALUE = DateHelper.MIN_DATE_VALUE;
        public const int MAX_DATE_VALUE = DateHelper.MAX_DATE_VALUE;
        public const int MAX_EXCEL_ROWS = 1048575;

        public static class ImportParameters
        {
            public const string FROM_MONTH = "fromMonth";
            public const string TO_MONTH = "toMonth";
            public const string HS_CODE = "hsCode";
            public const string PRODUCT = "product";
            public const string IEC = "iec";
            public const string IMPORTER = "importer";
            public const string FOREIGN_COUNTRY = "foreignCountry";
            public const string FOREIGN_NAME = "foreignName";
            public const string PORT = "port";
        }

        public static class StoredProcedureParameters
        {
            public const string SP_FROM_MONTH = "@fromMonth";
            public const string SP_TO_MONTH = "@ToMonth";
            public const string SP_HS_CODE = "@hs";
            public const string SP_PRODUCT = "@prod";
            public const string SP_IEC = "@Iec";
            public const string SP_IMPORTER = "@ImpCmp";
            public const string SP_FOREIGN_COUNTRY = "@forcount";
            public const string SP_FOREIGN_NAME = "@forname";
            public const string SP_PORT = "@port";
        }

        public static bool IsValidDateFormat(string dateString) =>
            RapidZ.Core.Helpers.DateHelper.IsValidDateFormat(dateString);

        public static bool IsValidDateRange(string fromMonth, string toMonth) =>
            RapidZ.Core.Helpers.DateHelper.IsValidDateRange(fromMonth, toMonth);

        public static string NormalizeParameter(string parameter) =>
            BaseParameterHelper.NormalizeParameter(parameter);

        public static List<string> ParseFilterList(string rawText)
        {
            return BaseParameterHelper.ParseFilterList(rawText);
        }

        public static Dictionary<string,string> CreateImportParameterSet(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string foreignCountry, string foreignName, string port) =>
            new()
            {
                { ImportParameters.FROM_MONTH, NormalizeParameter(fromMonth) },
                { ImportParameters.TO_MONTH, NormalizeParameter(toMonth) },
                { ImportParameters.HS_CODE, NormalizeParameter(hsCode) },
                { ImportParameters.PRODUCT, NormalizeParameter(product) },
                { ImportParameters.IEC, NormalizeParameter(iec) },
                { ImportParameters.IMPORTER, NormalizeParameter(importer) },
                { ImportParameters.FOREIGN_COUNTRY, NormalizeParameter(foreignCountry) },
                { ImportParameters.FOREIGN_NAME, NormalizeParameter(foreignName) },
                { ImportParameters.PORT, NormalizeParameter(port) },
            };

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public Dictionary<string,string> NormalizedParameters { get; set; } = new();
        }

        public static ValidationResult ValidateImportParameters(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string foreignCountry, string foreignName, string port)
        {
            var result = new ValidationResult();
            if (!IsValidDateFormat(fromMonth)) result.Errors.Add($"Invalid fromMonth format: {fromMonth}. Expected YYYYMM.");
            if (!IsValidDateFormat(toMonth)) result.Errors.Add($"Invalid toMonth format: {toMonth}. Expected YYYYMM.");
            if (IsValidDateFormat(fromMonth) && IsValidDateFormat(toMonth) && !IsValidDateRange(fromMonth, toMonth))
                result.Errors.Add($"Invalid date range: fromMonth ({fromMonth}) must be <= toMonth ({toMonth}).");
            result.NormalizedParameters = CreateImportParameterSet(fromMonth, toMonth, hsCode, product, iec, importer, foreignCountry, foreignName, port);
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
}