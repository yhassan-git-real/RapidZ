using System;
using System.Diagnostics;
using RapidZ.Core.Logging.Abstractions;

namespace RapidZ.Core.Logging.Core
{
    /// <summary>
    /// Implementation of operation timer for tracking elapsed time of operations
    /// </summary>
    public sealed class OperationTimer : IOperationTimer
    {
        private readonly Stopwatch _stopwatch;
        private readonly IModuleLogger _logger;
        private readonly string _operationName;
        private readonly string _processId;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the OperationTimer class
        /// </summary>
        /// <param name="logger">The module logger to use for logging completion</param>
        /// <param name="operationName">The name of the operation being timed</param>
        /// <param name="processId">The process ID associated with this operation</param>
        public OperationTimer(IModuleLogger logger, string operationName, string processId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _processId = processId ?? throw new ArgumentNullException(nameof(processId));
            
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the elapsed time since the timer was started
        /// </summary>
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <summary>
        /// Gets the name of the operation being timed
        /// </summary>
        public string OperationName => _operationName;

        /// <summary>
        /// Gets the process ID associated with this operation
        /// </summary>
        public string ProcessId => _processId;

        /// <summary>
        /// Stops the timer and logs the completion of the operation
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            _logger.LogProcessComplete(_operationName, _stopwatch.Elapsed, "Completed", _processId);
            _disposed = true;
        }
    }
}