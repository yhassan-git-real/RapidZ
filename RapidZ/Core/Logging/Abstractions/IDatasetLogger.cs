using RapidZ.Core.Logging.Models;

namespace RapidZ.Core.Logging.Abstractions
{
    /// <summary>
    /// Interface for logging dataset-specific operations and skipped datasets
    /// </summary>
    public interface IDatasetLogger
    {
        /// <summary>
        /// Logs a skipped dataset with detailed information
        /// </summary>
        /// <param name="datasetInfo">Information about the skipped dataset</param>
        void LogSkippedDataset(SkippedDatasetInfo datasetInfo);

        /// <summary>
        /// Logs a processing summary for datasets
        /// </summary>
        /// <param name="summary">Summary of dataset processing</param>
        void LogProcessingSummary(DatasetProcessingSummary summary);
    }
}