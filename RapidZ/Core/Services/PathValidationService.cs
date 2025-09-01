using System;
using System.IO;
using System.Threading.Tasks;
using RapidZ.Core.Services;

namespace RapidZ.Core.Services
{
    /// <summary>
    /// Service for validating file paths and directories
    /// </summary>
    public class PathValidationService
    {
        /// <summary>
        /// Validates if a directory path exists and is accessible
        /// </summary>
        /// <param name="path">The directory path to validate</param>
        /// <returns>Validation result with success status and error message</returns>
        public async Task<PathValidationResult> ValidateDirectoryPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new PathValidationResult
                {
                    IsValid = true, // Empty path is valid (will use default)
                    ErrorMessage = string.Empty
                };
            }

            try
            {
                // Run validation on background thread to avoid blocking UI
                return await Task.Run(() => ValidateDirectoryPath(path));
            }
            catch (Exception ex)
            {
                return new PathValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Synchronous directory path validation
        /// </summary>
        /// <param name="path">The directory path to validate</param>
        /// <returns>Validation result</returns>
        private PathValidationResult ValidateDirectoryPath(string path)
        {
            try
            {
                // Check if path format is valid
                if (!IsValidPathFormat(path))
                {
                    return new PathValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid path format. Please enter a valid directory path."
                    };
                }

                // Check if directory exists
                if (!Directory.Exists(path))
                {
                    return new PathValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Directory does not exist. Please enter an existing directory path."
                    };
                }

                // Check if directory is accessible (try to get directory info)
                var directoryInfo = new DirectoryInfo(path);
                _ = directoryInfo.GetDirectories(); // This will throw if not accessible

                return new PathValidationResult
                {
                    IsValid = true,
                    ErrorMessage = string.Empty
                };
            }
            catch (UnauthorizedAccessException)
            {
                return new PathValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Access denied. Please check directory permissions."
                };
            }
            catch (DirectoryNotFoundException)
            {
                return new PathValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Directory not found. Please enter a valid directory path."
                };
            }
            catch (Exception ex)
            {
                return new PathValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Path validation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Checks if the path format is valid
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if format is valid</returns>
        private bool IsValidPathFormat(string path)
        {
            try
            {
                // This will throw if path contains invalid characters
                Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result of path validation operation
    /// </summary>
    public class PathValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}