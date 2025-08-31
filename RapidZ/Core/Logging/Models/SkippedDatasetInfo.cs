namespace RapidZ.Core.Logging.Models
{
    /// <summary>
    /// Represents information about a skipped dataset
    /// </summary>
    public sealed class SkippedDatasetInfo
    {
        /// <summary>
        /// Gets or sets the module type (Export/Import)
        /// </summary>
        public string ModuleType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the combination number
        /// </summary>
        public int CombinationNumber { get; set; }

        /// <summary>
        /// Gets or sets the row count that would have been processed
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Gets or sets the reason for skipping the dataset
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the process parameters for the skipped dataset
        /// </summary>
        public ProcessParameters Parameters { get; set; } = new();

        /// <summary>
        /// Gets a formatted reason description based on the reason and row count
        /// </summary>
        public string FormattedReason
        {
            get
            {
                return Reason.ToLowerInvariant() switch
                {
                    "rowlimit" => $"{RowCount:N0} (Exceeds Excel limit of 1,048,576)",
                    "nodata" => "0 (No data returned)",
                    _ => $"{RowCount:N0}"
                };
            }
        }
    }
}