using System;

namespace RapidZ.Core.Cancellation
{
    /// <summary>
    /// Interface for managing cancellation operations across the application
    /// </summary>
    public interface ICancellationManager
    {
        /// <summary>
        /// Gets the current cancellation token
        /// </summary>
        CancellationToken Token { get; }

        /// <summary>
        /// Gets whether cancellation has been requested
        /// </summary>
        bool IsCancellationRequested { get; }

        /// <summary>
        /// Event raised when cancellation is requested
        /// </summary>
        event EventHandler? CancellationRequested;

        /// <summary>
        /// Starts a new operation with a fresh cancellation token
        /// </summary>
        /// <param name="operationName">Name of the operation being started</param>
        /// <param name="processId">Optional process ID for logging</param>
        void StartOperation(string operationName, string? processId = null);

        /// <summary>
        /// Requests cancellation of the current operation
        /// </summary>
        /// <param name="reason">Reason for cancellation</param>
        /// <param name="processId">Optional process ID for logging</param>
        void RequestCancellation(string? reason = null, string? processId = null);

        /// <summary>
        /// Marks the current operation as completed
        /// </summary>
        /// <param name="operationName">Name of the operation being completed</param>
        /// <param name="processId">Optional process ID for logging</param>
        void CompleteOperation(string operationName, string? processId = null);

        /// <summary>
        /// Throws OperationCanceledException if cancellation has been requested
        /// </summary>
        void ThrowIfCancellationRequested();
    }
}