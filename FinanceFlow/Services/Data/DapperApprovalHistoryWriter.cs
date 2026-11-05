using System.Data;
using Dapper;
using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinanceFlow.Services.Data;

public class DapperApprovalHistoryWriter(
    ApplicationDbContext context)
    : IApprovalHistoryWriter
{
    public async Task InsertAsync(ApprovalHistory history)
    {
        var connection = context.Database.GetDbConnection();

        var transaction = context.Database.CurrentTransaction?
            .GetDbTransaction();

        await connection.ExecuteAsync(
            "dbo.usp_ApprovalHistory_Insert",
            new
            {
                history.FinanceRequestId,
                FromStatus = (int)history.FromStatus,
                ToStatus = (int)history.ToStatus,
                history.ActionByUserId,
                history.Comment,
                history.ActionAt
            },
            transaction: transaction,
            commandType: CommandType.StoredProcedure);
    }
}