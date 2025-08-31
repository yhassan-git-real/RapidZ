using System;

namespace RapidZ.Core.Logging.Abstractions
{
    /// <summary>
    /// Interface for timing operations and automatically logging their completion
    /// </summary>
    public interface IOperationTimer : IDisposable
    {
        /// <summary>
        /// Gets the elapsed time since the timer was started
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Gets the name of the operation being timed
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Gets the process ID associated with this timer
        /// </summary>
        string ProcessId { get; }
    }
}