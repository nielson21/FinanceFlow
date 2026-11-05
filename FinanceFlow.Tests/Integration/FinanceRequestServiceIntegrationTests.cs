using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration
{
    public class FinanceRequestServiceIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        [Fact]
        public async Task FinanceRequestService_Approve_ShouldSaveStatusAndHistory()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestor = CreateUser("test-user", "requestor@test.com");
            context.Users.Add(requestor);

            var approver = CreateUser("approver-user", "approver@test.com");
            context.Users.Add(approver);

            var request = CreateFinanceRequest("FR-APPROVE-001", RequestStatus.PendingApproval, "test-user");
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.ApproveAsync(request.Id, approver.Id, "Approved test");

            context.ChangeTracker.Clear();

            var requestWithHistory = await context.FinanceRequests.Include(x => x.ApprovalHistories)
                .SingleAsync(x => x.ReferenceNo == "FR-APPROVE-001");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(RequestStatus.Approved, requestWithHistory.Status);
            var history = Assert.Single(requestWithHistory.ApprovalHistories);
            Assert.Equal(RequestStatus.PendingApproval, history.FromStatus);
            Assert.Equal(RequestStatus.Approved, history.ToStatus);
            Assert.Equal(approver.Id, history.ActionByUserId);
            Assert.Equal("Approved test", history.Comment);
        }

        [Fact]
        public async Task FinanceRequestService_ApproveDraftRequest_ShouldFail()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestor = CreateUser("test-user", "requestor@test.com");
            context.Users.Add(requestor);

            var approver = CreateUser("approver-user", "approver@test.com");
            context.Users.Add(approver);

            var request = CreateFinanceRequest("FR-DRAFT-001", RequestStatus.Draft, "test-user");
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.ApproveAsync(request.Id, approver.Id, "Approved draft test");

            context.ChangeTracker.Clear();

            var requestWithHistory = await context.FinanceRequests.Include(x => x.ApprovalHistories)
                .SingleAsync(x => x.ReferenceNo == "FR-DRAFT-001");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(RequestStatus.Draft, requestWithHistory.Status);
            Assert.Empty(requestWithHistory.ApprovalHistories);
        }

        [Fact]
        public async Task FinanceRequestService_Reject_ShouldSaveStatusAndHistory()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var referenceNo = "FR-REJECT-001";
            var comment = "Rejected test";
            var requestorId = "test-user";
            var approverId = "approver-user";
            var requestorEmail = "requestor@test.com";
            var approverEmail = "approver@test.com";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var approver = CreateUser(approverId, approverEmail);
            context.Users.Add(approver);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.PendingApproval, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.RejectAsync(request.Id, approverId, comment);

            context.ChangeTracker.Clear();

            var requestWithHistory = await context.FinanceRequests.Include(x => x.ApprovalHistories)
                .SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(RequestStatus.Rejected, requestWithHistory.Status);
            var history = Assert.Single(requestWithHistory.ApprovalHistories);
            Assert.Equal(RequestStatus.PendingApproval, history.FromStatus);
            Assert.Equal(RequestStatus.Rejected, history.ToStatus);
            Assert.Equal(approverId, history.ActionByUserId);
            Assert.Equal(comment, history.Comment);
        }

        [Fact]
        public async Task FinanceRequestService_RejectApprovedRequest_ShouldFail()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var referenceNo = "FR-REJECT-001";
            var comment = "Reject approved test";
            var requestorId = "test-user";
            var approverId = "approver-user";
            var requestorEmail = "requestor@test.com";
            var approverEmail = "approver@test.com";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var approver = CreateUser(approverId, approverEmail);
            context.Users.Add(approver);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.Approved, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.RejectAsync(request.Id, approverId, comment);

            context.ChangeTracker.Clear();

            var requestWithHistory = await context.FinanceRequests.Include(x => x.ApprovalHistories)
                .SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(RequestStatus.Approved, requestWithHistory.Status);
            Assert.Empty(requestWithHistory.ApprovalHistories);
        }

        [Fact]
        public async Task FinanceRequestService_ApproveNonExistingRequest_ShouldFail()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var comment = "Approve non-existing test";
            var approverId = "approver-user";
            var requestId = 999;

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.ApproveAsync(requestId, approverId, comment);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(context.ApprovalHistories);
        }

        [Fact]
        public async Task FinanceRequestService_Submit_ShouldSetPendingApprovalAndCreateHistory()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var referenceNo = "FR-SUBMIT-001";
            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.Draft, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.SubmitAsync(request.Id, requestorId);

            context.ChangeTracker.Clear();

            var requestWithHistory = await context.FinanceRequests.Include(x => x.ApprovalHistories)
                .SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(RequestStatus.PendingApproval, requestWithHistory.Status);
            var history = Assert.Single(requestWithHistory.ApprovalHistories);
            Assert.Equal(RequestStatus.Draft, history.FromStatus);
            Assert.Equal(RequestStatus.PendingApproval, history.ToStatus);
            Assert.Equal(requestorId, history.ActionByUserId);
        }

        [Fact]
        public async Task FinanceRequestService_Create_ShouldSaveDraftRequest()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var title = "Create test";
            var amount = 1000m;

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            // Act
            await service.CreateAsync(title, amount, requestorId);

            context.ChangeTracker.Clear();

            var request = await context.FinanceRequests.SingleAsync();

            // Assert
            Assert.Equal(title, request.Title);
            Assert.Equal(amount, request.Amount);
            Assert.Equal(RequestStatus.Draft, request.Status);
            Assert.Equal(requestorId, request.RequestorId);
            Assert.False(string.IsNullOrWhiteSpace(request.ReferenceNo));
            Assert.StartsWith($"FR-{timeProvider.GetUtcNow().Year}-", request.ReferenceNo);
        }

        [Fact]
        public async Task FinanceRequestService_SubmitByAnotherUser_ShouldFail()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var referenceNo = "FR-SUBMIT-001";
            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var anotherRequestorId = "another-test";
            var anotherRequestorEmail = "another@test.com";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var anotherRequestor = CreateUser(anotherRequestorId, anotherRequestorEmail);
            context.Users.Add(anotherRequestor);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.Draft, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            var result = await service.SubmitAsync(request.Id, anotherRequestorId);

            context.ChangeTracker.Clear();

            var reloadedRequest = await context.FinanceRequests.SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(RequestStatus.Draft, reloadedRequest.Status);
            Assert.False(await context.ApprovalHistories.AnyAsync(x => x.FinanceRequestId == reloadedRequest.Id));
        }

        [Fact]
        public async Task FinanceRequestService_Approve_WhenHistorySaveFails_ShouldRollbackStatus()
        {
            await ResetDatabaseAsync(factory.Services);

            // Arrange
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var referenceNo = "FR-APPROVE-001";
            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var nonExistingApproverId = "non-existing-approver";
            var comment = "Test";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.PendingApproval, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();


            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(
                () => service.ApproveAsync(request.Id, nonExistingApproverId, comment));

            context.ChangeTracker.Clear();
            var reloadedRequest = await context.FinanceRequests.SingleAsync(x => x.ReferenceNo == referenceNo);

            Assert.Equal(RequestStatus.PendingApproval, reloadedRequest.Status);
            Assert.False(await context.ApprovalHistories.AnyAsync(x => x.FinanceRequestId == reloadedRequest.Id));
        }

        [Fact]
        public async Task FinanceRequestService_Create_WhenReferenceDuplicate_ShouldRetry()
        {
            // Arrange
            var customFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IRequestReferenceGenerator>();

                    services.AddTransient<IRequestReferenceGenerator>(_ =>
                        new SequenceRequestReferenceGenerator(
                            "FR-DUPLICATE-001",
                            "FR-UNIQUE-002"));
                });
            });

            await ResetDatabaseAsync(customFactory.Services);

            using var scope = customFactory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var referenceNo = "FR-DUPLICATE-001";
            var title = "Create test";
            var amount = 1000m;

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.Draft, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act
            await service.CreateAsync(title, amount, requestorId);

            context.ChangeTracker.Clear();

            var requests = await context.FinanceRequests.ToListAsync();

            // Assert
            Assert.Equal(2, requests.Count);
            Assert.Contains(requests, x => x.ReferenceNo == referenceNo);
            Assert.Contains(requests, x => x.ReferenceNo == "FR-UNIQUE-002");
        }

        [Fact]
        public async Task FinanceRequestService_Create_WhenAllReferencesDuplicate_ShouldThrow()
        {
            // Arrange
            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            string[] referenceNos = ["FR-DUPLICATE-001", "FR-DUPLICATE-002", "FR-DUPLICATE-003"];
            var title = "Create test";
            var amount = 1000m;

            var customFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IRequestReferenceGenerator>();

                    services.AddTransient<IRequestReferenceGenerator>(_ =>
                        new SequenceRequestReferenceGenerator(referenceNos));
                });
            });

            await ResetDatabaseAsync(customFactory.Services);

            using var scope = customFactory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            foreach (var referenceNo in referenceNos)
            {
                var request = CreateFinanceRequest(referenceNo, RequestStatus.Draft, requestorId);
                context.FinanceRequests.Add(request);
            }

            await context.SaveChangesAsync();

            var service = scope.ServiceProvider.GetRequiredService<IFinanceRequestService>();

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                service.CreateAsync(title, amount, requestorId));

            context.ChangeTracker.Clear();

            Assert.Equal(3, await context.FinanceRequests.CountAsync());
        }

        [Fact]
        public async Task Approve_WhenHistoryInsertFails_ShouldRollbackStatusChange()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "requestor-user";
            const string requestorEmail = "requestor@test.com";
            const string nonExistingApproverId = "non-existing-approver";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(
                    context,
                    requestorId,
                    requestorEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-TRANSACTION-001",
                    RequestStatus.PendingApproval,
                    requestorId,
                    "Transaction Test",
                    5000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            // Act
            using (var scope = factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IFinanceRequestService>();

                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    service.ApproveAsync(
                        requestId,
                        nonExistingApproverId,
                        "Approve test"));
            }

            // Assert
            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var savedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            var histories = await assertContext.ApprovalHistories
                .Where(x => x.FinanceRequestId == requestId)
                .ToListAsync();

            Assert.Equal(
                RequestStatus.PendingApproval,
                savedRequest.Status);

            Assert.Empty(histories);
        }
    }
}
