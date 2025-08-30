using System;
using System.Threading;
using System.Threading.Tasks;
using RapidZ.Core.Models;

namespace RapidZ.Core.Controllers
{
    /// <summary>
    /// Interface for import controller operations
    /// </summary>
    public interface IImportController
    {
        /// <summary>
        /// Runs the import process asynchronously
        /// </summary>
        /// <param name="importInputs">The import input parameters</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <param name="selectedView">The selected database view name</param>
        /// <param name="selectedStoredProcedure">The selected stored procedure name</param>
        /// <returns>Task representing the async operation</returns>
        Task RunAsync(ImportInputs importInputs, CancellationToken cancellationToken, string selectedView, string selectedStoredProcedure);
    }
}