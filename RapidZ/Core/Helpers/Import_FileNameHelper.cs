using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RapidZ.Core.Helpers
{
    /// <summary>
    /// Helper class for generating import file names with consistent formatting
    /// </summary>
    public static class Import_FileNameHelper
    {
        /// <summary>
        /// Generates a standardized import file name based on the provided parameters
        /// </summary>
        public static string GenerateImportFileName(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port,
            string fileSuffix = "IMP")
        {
            // Use BaseFileNameHelper for consistent month range formatting
            var monthRange = BaseFileNameHelper.BuildMonthRangeSegment(fromMonth, toMonth);
            
            // Build core file name using BaseFileNameHelper
            var parameters = new[] { monthRange, hsCode, product, iec, importer, country, name, port, fileSuffix };
            var coreFileName = BaseFileNameHelper.BuildCoreFileName(parameters) + ".xlsx";
            
            return coreFileName;
        }

        /// <summary>
        /// Legacy method - now delegates to BaseFileNameHelper for consistency
        /// </summary>
        [Obsolete("Use GenerateImportFileName instead. This method is maintained for backward compatibility.")]
        public static string BuildImportFileName(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port,
            string fileSuffix = "IMP")
        {
            return GenerateImportFileName(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, fileSuffix);
        }

        /// <summary>
        /// Legacy method - now delegates to BaseFileNameHelper for consistency
        /// </summary>
        [Obsolete("Use BaseFileNameHelper.BuildMonthRangeSegment instead. This method is maintained for backward compatibility.")]
        public static string BuildMonthRange(string fromMonth, string toMonth)
        {
            return BaseFileNameHelper.BuildMonthRangeSegment(fromMonth, toMonth);
        }

        /// <summary>
        /// Validates if the generated file name is valid for the file system
        /// </summary>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !fileName.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Gets the file extension for import files
        /// </summary>
        public static string GetImportFileExtension()
        {
            return ".xlsx";
        }

        /// <summary>
        /// Generates a complete file path for import files
        /// </summary>
        public static string GenerateImportFilePath(
            string outputDirectory, string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port,
            string fileSuffix = "IMP")
        {
            var fileName = GenerateImportFileName(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, fileSuffix);
            return Path.Combine(outputDirectory, fileName);
        }
    }
}