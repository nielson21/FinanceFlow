using System.Data;
using Dapper;
using FinanceFlow.Data;
using FinanceFlow.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Services.Data;

public class DapperFinanceRequestSummaryReader(ApplicationDbContext context) : IFinanceRequestSummaryReader
{
    public async Task<FinanceRequestDashboardSummary> GetByRequestorAsync(string userId)
    {
        var connection = context.Database.GetDbConnection();

        return await connection.QuerySingleAsync<FinanceRequestDashboardSummary>(
            "dbo.usp_FinanceRequest_GetSummaryByRequestor",
            new { RequestorId = userId },
            commandType: CommandType.StoredProcedure);
    }
}