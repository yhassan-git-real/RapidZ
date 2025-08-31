namespace RapidZ.Core.Logging.Models
{
    /// <summary>
    /// Represents a summary of dataset processing operations
    /// </summary>
    public sealed class DatasetProcessingSummary
    {
        /// <summary>
        /// Gets or sets the module type (Export/Import)
        /// </summary>
        public string ModuleType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of combinations processed
        /// </summary>
        public int TotalCombinations { get; set; }

        /// <summary>
        /// Gets or sets the number of files successfully generated
        /// </summary>
        public int FilesGenerated { get; set; }

        /// <summary>
        /// Gets or sets the number of combinations that were skipped
        /// </summary>
        public int CombinationsSkipped { get; set; }

        /// <summary>
        /// Calculates the success rate as a percentage
        /// </summary>
        public double SuccessRate => TotalCombinations > 0 ? (double)FilesGenerated / TotalCombinations * 100 : 0;

        /// <summary>
        /// Gets a formatted string representation of the summary
        /// </summary>
        /// <returns>A formatted summary string</returns>
        public override string ToString()
        {
            return $"{ModuleType.ToUpper()} - Total: {TotalCombinations}, Generated: {FilesGenerated}, Skipped: {CombinationsSkipped}, Success Rate: {SuccessRate:F1}%";
        }
    }
}