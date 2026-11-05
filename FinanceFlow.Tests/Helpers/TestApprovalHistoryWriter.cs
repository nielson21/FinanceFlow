using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services.Data;

namespace FinanceFlow.Tests.Helpers
{
    public class TestApprovalHistoryWriter(ApplicationDbContext context) : IApprovalHistoryWriter
    {
        public async Task InsertAsync(ApprovalHistory history)
        {
            await context.ApprovalHistories.AddAsync(history);

            await context.SaveChangesAsync();
        }
    }
}
