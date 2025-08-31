using System;

namespace RapidZ.Core.Logging.Models
{
    /// <summary>
    /// Represents a summary of processing operations
    /// </summary>
    public sealed class ProcessingSummary
    {
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
        /// Gets or sets the total elapsed time for processing
        /// </summary>
        public TimeSpan TotalElapsed { get; set; }

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
            return $"Total: {TotalCombinations}, Generated: {FilesGenerated}, Skipped: {CombinationsSkipped}, Success Rate: {SuccessRate:F1}%, Time: {TotalElapsed:hh\\:mm\\:ss}";
        }
    }
}