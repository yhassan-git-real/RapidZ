using System.Collections.Generic;
using System.Linq;

namespace RapidZ.Core.Parameters
{
    /// <summary>
    /// Base parameter helper providing shared functionality for both Import and Export
    /// </summary>
    public static class BaseParameterHelper
    {
        public const string WILDCARD = "%";
        
        /// <summary>
        /// Normalizes parameter value (trims and handles empty strings)
        /// </summary>
        public static string NormalizeParameter(string parameter) =>
            string.IsNullOrWhiteSpace(parameter) ? WILDCARD : parameter.Trim();
            
        /// <summary>
        /// Parses a comma-separated filter list and returns normalized values
        /// </summary>
        public static List<string> ParseFilterList(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return new List<string> { WILDCARD };
                
            // If there are no commas, return as a single item list
            if (!rawText.Contains(','))
                return new List<string> { rawText.Trim() };
                
            // Split by comma, trim each value, and ensure no empty values
            var result = rawText.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
                
            // If after processing we have no items, return a "%" wildcard
            if (result.Count == 0)
                result.Add(WILDCARD);
                
            return result;
        }
        
        /// <summary>
        /// Filters and sanitizes parameters, removing null, empty, whitespace, and wildcard values.
        /// </summary>
        public static string[] FilterAndSanitizeParameters(string[] parameters, string wildcard = "%")
        {
            return parameters
                .Where(p => !string.IsNullOrWhiteSpace(p) && p != wildcard)
                .Select(p => SanitizeParameter(p))
                .ToArray();
        }
        
        /// <summary>
        /// Sanitizes a parameter string for use in file names.
        /// </summary>
        public static string SanitizeParameter(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return parameter;

            string sanitized = parameter.Trim();
            sanitized = sanitized.Replace(' ', '_');
            
            // Remove any remaining illegal characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// Creates a parameter summary string for logging
        /// </summary>
        public static string FormatParametersForDisplay(Dictionary<string,string> parameters) =>
            string.Join(", ", parameters.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
    }
}
