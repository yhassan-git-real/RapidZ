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
        /// Generates a standardized import file name based on the provided parameters,
        /// matching the export file naming convention
        /// </summary>
        public static string GenerateImportFileName(
            string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port,
            string fileSuffix = "IMP")
        {
            // Build month range segment using DateHelper for consistency with export
            string monthRange = DateHelper.BuildMonthRangeSegment(fromMonth, toMonth, "MMM");

            // Build core file name from parameters, matching export pattern
            string[] parameters = { hsCode, product, iec, importer, country, name, port };
            string core = BaseFileNameHelper.BuildCoreFileName(parameters, "%", "ALL");

            return $"{core}_{monthRange}{fileSuffix}.xlsx";
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
        /// Uses the standardized naming convention matching export files
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