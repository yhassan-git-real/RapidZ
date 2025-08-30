using System.Threading.Tasks;

namespace RapidZ.Core.Services
{
    public interface ILogParserService
    {
        Task<ExecutionSummary> GetLatestExecutionSummaryAsync(string mode = "Export");
    }
}
