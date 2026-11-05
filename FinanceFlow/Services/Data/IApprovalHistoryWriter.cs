using FinanceFlow.Models;

namespace FinanceFlow.Services.Data;

public interface IApprovalHistoryWriter
{
    Task InsertAsync(ApprovalHistory history);
}