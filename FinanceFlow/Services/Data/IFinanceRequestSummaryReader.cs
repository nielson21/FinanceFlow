using FinanceFlow.Services.Models;

namespace FinanceFlow.Services.Data
{
    public interface IFinanceRequestSummaryReader
    {
        Task<FinanceRequestDashboardSummary> GetByRequestorAsync(string userId);
    }
}
