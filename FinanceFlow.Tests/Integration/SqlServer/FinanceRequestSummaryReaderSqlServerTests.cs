using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services;
using FinanceFlow.Services.Data;
using FinanceFlow.Tests.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration.SqlServer;

public class FinanceRequestSummaryReaderSqlServerTests(
    SqlServerFinanceFlowWebApplicationFactory factory)
    : IClassFixture<SqlServerFinanceFlowWebApplicationFactory>
{
    [Fact]
    public async Task GetByRequestorAsync_ShouldReturnSummaryFromStoredProcedure()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(
                context,
                requestorId,
                requestorEmail);

            CreateAndAddFinanceRequest(
                context,
                "FR-SP-001",
                RequestStatus.Draft,
                requestorId,
                "Draft Request",
                1000m);

            CreateAndAddFinanceRequest(
                context,
                "FR-SP-002",
                RequestStatus.PendingApproval,
                requestorId,
                "Pending Request",
                2000m);

            CreateAndAddFinanceRequest(
                context,
                "FR-SP-003",
                RequestStatus.Approved,
                requestorId,
                "Approved Request",
                3000m);

            CreateAndAddFinanceRequest(
                context,
                "FR-SP-004",
                RequestStatus.Rejected,
                requestorId,
                "Rejected Request",
                4000m);

            await context.SaveChangesAsync();
        }

        // Act
        using var actScope = factory.Services.CreateScope();

        var reader = actScope.ServiceProvider
            .GetRequiredService<IFinanceRequestSummaryReader>();

        var result = await reader.GetByRequestorAsync(requestorId);

        // Assert
        Assert.Equal(1, result.DraftCount);
        Assert.Equal(1, result.PendingApprovalCount);
        Assert.Equal(1, result.ApprovedCount);
        Assert.Equal(1, result.RejectedCount);

        Assert.Equal(10000m, result.TotalAmount);
        Assert.Equal(2000m, result.PendingApprovalAmount);
        Assert.Equal(3000m, result.ApprovedAmount);
    }

    [Fact]
    public async Task Approve_ShouldUpdateStatusAndInsertHistoryUsingStoredProcedure()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        const string requestorId = "requestor-user";
        const string approverId = "approver-user";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, "requestor@test.com");
            CreateAndAddUser(context, approverId, "approver@test.com");

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-SQL-APPROVE-001",
                RequestStatus.PendingApproval,
                requestorId,
                "Laptop Purchase",
                5000m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IFinanceRequestService>();

            var result = await service.ApproveAsync(
                requestId,
                approverId,
                "Approved for purchase");

            Assert.True(result.IsSuccess);
        }

        // Assert
        using var assertScope = factory.Services.CreateScope();

        var assertContext = assertScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var savedRequest = await assertContext.FinanceRequests
            .SingleAsync(x => x.Id == requestId);

        var history = await assertContext.ApprovalHistories
            .SingleAsync(x => x.FinanceRequestId == requestId);

        Assert.Equal(RequestStatus.Approved, savedRequest.Status);

        Assert.Equal(RequestStatus.PendingApproval, history.FromStatus);
        Assert.Equal(RequestStatus.Approved, history.ToStatus);
        Assert.Equal(approverId, history.ActionByUserId);
        Assert.Equal("Approved for purchase", history.Comment);
    }

    [Fact]
    public async Task Approve_WhenStoredProcedureFails_ShouldRollbackStatusChange()
    {
        // Arrange
        await factory.ResetDatabaseAsync();

        const string requestorId = "requestor-user";
        const string nonExistingApproverId = "non-existing-user";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, "requestor@test.com");

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-SQL-APPROVE-001",
                RequestStatus.PendingApproval,
                requestorId,
                "Laptop Purchase",
                5000m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IFinanceRequestService>();

            var ex = await Assert.ThrowsAsync<SqlException>(() =>
                service.ApproveAsync(
                    requestId,
                    nonExistingApproverId,
                    "Approved for purchase")
            );
            Assert.Equal(547, ex.Number);
        }

        // Assert
        using var assertScope = factory.Services.CreateScope();

        var assertContext = assertScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var savedRequest = await assertContext.FinanceRequests
            .SingleAsync(x => x.Id == requestId);

        Assert.Equal(RequestStatus.PendingApproval, savedRequest.Status);
        Assert.Equal(0, await assertContext.ApprovalHistories.CountAsync());
    }
}