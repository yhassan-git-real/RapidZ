using System;
using System.Threading;
using System.Threading.Tasks;
using RapidZ.Core.Logging.Services;

namespace RapidZ.Core.Cancellation
{
    /// <summary>
    /// Manages cancellation operations across the application
    /// Provides thread-safe cancellation coordination
    /// </summary>
    public class CancellationManager : ICancellationManager, IDisposable
    {
        private readonly object _lock = new object();
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _currentOperationName;
        private bool _disposed = false;

        /// <summary>
        /// Gets the current cancellation token
        /// </summary>
        public CancellationToken Token
        {
            get
            {
                lock (_lock)
                {
                    return _cancellationTokenSource?.Token ?? CancellationToken.None;
                }
            }
        }

        /// <summary>
        /// Gets whether cancellation has been requested
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock (_lock)
                {
                    return _cancellationTokenSource?.IsCancellationRequested ?? false;
                }
            }
        }

        /// <summary>
        /// Event raised when cancellation is requested
        /// </summary>
        public event EventHandler? CancellationRequested;

        /// <summary>
        /// Starts a new operation with a fresh cancellation token
        /// </summary>
        /// <param name="operationName">Name of the operation being started</param>
        /// <param name="processId">Optional process ID for logging</param>
        public void StartOperation(string operationName, string? processId = null)
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CancellationManager));

                // Dispose existing token source if any
                _cancellationTokenSource?.Dispose();

                // Create new token source
                _cancellationTokenSource = new CancellationTokenSource();
                _currentOperationName = operationName;

                var logger = LoggerFactory.GetCancellationLogger();
                logger.LogInfo($"Operation started: {operationName}", processId);
            }
        }

        /// <summary>
        /// Requests cancellation of the current operation
        /// </summary>
        /// <param name="reason">Reason for cancellation</param>
        /// <param name="processId">Optional process ID for logging</param>
        public void RequestCancellation(string? reason = null, string? processId = null)
        {
            lock (_lock)
            {
                if (_disposed || _cancellationTokenSource == null)
                    return;

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var logMessage = string.IsNullOrWhiteSpace(reason)
                        ? $"Cancellation requested for operation: {_currentOperationName ?? "Unknown"}"
                        : $"Cancellation requested for operation: {_currentOperationName ?? "Unknown"} - Reason: {reason}";

                    var logger = LoggerFactory.GetCancellationLogger();
                    logger.LogWarning(logMessage, processId);

                    _cancellationTokenSource.Cancel();

                    // Raise the event outside the lock to avoid potential deadlocks
                    Task.Run(() => CancellationRequested?.Invoke(this, EventArgs.Empty));
                }
            }
        }

        /// <summary>
        /// Marks the current operation as completed
        /// </summary>
        /// <param name="operationName">Name of the operation being completed</param>
        /// <param name="processId">Optional process ID for logging</param>
        public void CompleteOperation(string operationName, string? processId = null)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                var logger = LoggerFactory.GetCancellationLogger();
                logger.LogInfo($"Operation completed: {operationName}", processId);
                _currentOperationName = null;
            }
        }

        /// <summary>
        /// Throws OperationCanceledException if cancellation has been requested
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Disposes the cancellation manager and its resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        /// <param name="disposing">Whether disposing from Dispose method</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lock)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    _currentOperationName = null;
                    _disposed = true;
                }
            }
        }
    }
}