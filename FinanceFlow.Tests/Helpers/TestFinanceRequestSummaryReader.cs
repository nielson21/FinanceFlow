using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services.Data;
using FinanceFlow.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Tests.Helpers;

public class TestFinanceRequestSummaryReader(ApplicationDbContext context) : IFinanceRequestSummaryReader
{
    public async Task<FinanceRequestDashboardSummary> GetByRequestorAsync(string userId)
    {
        return await context.FinanceRequests
            .Where(x => x.RequestorId == userId)
            .GroupBy(x => 1)
            .Select(g => new FinanceRequestDashboardSummary
            {
                DraftCount = g.Count(x => x.Status == RequestStatus.Draft),

                PendingApprovalCount = g.Count(
                    x => x.Status == RequestStatus.PendingApproval),

                ApprovedCount = g.Count(
                    x => x.Status == RequestStatus.Approved),

                RejectedCount = g.Count(
                    x => x.Status == RequestStatus.Rejected),

                TotalAmount = g.Sum(x => x.Amount),

                PendingApprovalAmount = g.Sum(
                    x => x.Status == RequestStatus.PendingApproval
                        ? x.Amount
                        : 0m),

                ApprovedAmount = g.Sum(
                    x => x.Status == RequestStatus.Approved
                        ? x.Amount
                        : 0m)
            })
            .SingleOrDefaultAsync()
            ?? new FinanceRequestDashboardSummary();
    }
}