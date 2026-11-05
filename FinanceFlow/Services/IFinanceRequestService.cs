using FinanceFlow.Models;
using FinanceFlow.Services.Models;
using FinanceFlow.Services.Results;

namespace FinanceFlow.Services
{
    public interface IFinanceRequestService
    {
        Task<ServiceResult> SubmitAsync(int id, string userId);
        Task<ServiceResult> ApproveAsync(int id, string userId, string? comment);
        Task<ServiceResult> RejectAsync(int id, string userId, string? comment);
        Task<FinanceRequest> CreateAsync(string title, decimal amount, string userId);
        Task<FinanceRequest?> GetByIdAsync(int id);
        Task<FinanceRequest?> GetByIdForRequestorAsync(int id, string requestorId);
        Task<FinanceRequest?> GetEditableByRequestorAsync(int id, string requestorId);
        Task<ServiceResult> UpdateDraftAsync(int id, string requestorId, string title, decimal amount, byte[] rowVersion);
        Task<FinanceRequest?> GetDetailsByRequestorAsync(int id, string requestorId);
        Task<FinanceRequestDashboardSummary> GetDashboardSummaryAsync(string userId);
        Task<PaginatedResult<FinanceRequestListItem>> GetPagedByRequestorAsync(
            string userId,
            RequestStatus? status,
            string? search,
            int page,
            int pageSize);
        Task<List<FinanceRequestListItem>> GetByRequestorAsync(string requestorId);
    }
}