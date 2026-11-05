using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration
{
    public class ApprovalIntegrationTests (FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        [Fact]
        public async Task Approve_LargeAmount_WithoutClaim_ShouldBeForbidden()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string approverId = "test-user";
            const string approverEmail = "approver2@test.com";
            const string requestorId = "requestor-user";
            const string requestorEmail = "requestor@test.com";

            int requestId;
            const string referenceNo = "FR-TEST-001";
            const string title = "Server Infrastructure Upgrade";
            const decimal amount = 150000m;

            const string role = "Approver";
            const string tokenUri = "/Approvals";

            const string postUri = "/Approvals/Approve";
            const string approvalComment = "Approved";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, approverId, approverEmail);
                CreateAndAddUser(context, requestorId, requestorEmail);

                var financeRequest = CreateAndAddFinanceRequest(context, referenceNo, Models.RequestStatus.PendingApproval,
                    requestorId, title, amount);

                await context.SaveChangesAsync();

                requestId = financeRequest.Id;
            }

            var client = GetHttpClient(factory, role);

            var content = await GenerateContentAsync(client, tokenUri,
                new Dictionary<string, string>
                {
                    ["Id"] = requestId.ToString(),
                    ["Comment"] = approvalComment
                });

            // Act
            var response = await client.PostAsync(postUri, content);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var reloadedRequest = await assertContext.FinanceRequests.SingleAsync(x => x.Id == requestId);

            Assert.Equal(RequestStatus.PendingApproval, reloadedRequest.Status);
        }

        [Fact]
        public async Task Approve_LargeAmount_WithClaim_ShouldApprove()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string approverId = "test-user";
            const string approverEmail = "approver2@test.com";
            const string requestorId = "requestor-user";
            const string requestorEmail = "requestor@test.com";

            int requestId;
            const string referenceNo = "FR-TEST-001";
            const string title = "Server Infrastructure Upgrade";
            const decimal amount = 150000m;

            const string role = "Approver";
            const string tokenUri = "/Approvals";

            const string postUri = "/Approvals/Approve";
            const string approvalComment = "Approved";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, approverId, approverEmail);
                CreateAndAddUser(context, requestorId, requestorEmail);

                var financeRequest = CreateAndAddFinanceRequest(context, referenceNo, Models.RequestStatus.PendingApproval,
                    requestorId, title, amount);

                await context.SaveChangesAsync();

                requestId = financeRequest.Id;
            }

            var client = GetHttpClient(factory, role, true);

            var content = await GenerateContentAsync(client, tokenUri,
                new Dictionary<string, string>
                {
                    ["Id"] = requestId.ToString(),
                    ["Comment"] = approvalComment
                });

            // Act
            var response = await client.PostAsync(postUri, content);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var reloadedRequest = await assertContext.FinanceRequests.SingleAsync(x => x.Id == requestId);

            Assert.Equal(RequestStatus.Approved, reloadedRequest.Status);
        }

        [Fact]
        public async Task Approve_NormalAmount_WithoutClaim_ShouldApprove()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string approverId = "test-user";
            const string approverEmail = "approver2@test.com";
            const string requestorId = "requestor-user";
            const string requestorEmail = "requestor@test.com";

            int requestId;
            const string referenceNo = "FR-TEST-001";
            const string title = "Server Infrastructure Upgrade";
            const decimal amount = 50000m;

            const string role = "Approver";
            const string tokenUri = "/Approvals";

            const string postUri = "/Approvals/Approve";
            const string approvalComment = "Approved";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, approverId, approverEmail);
                CreateAndAddUser(context, requestorId, requestorEmail);

                var financeRequest = CreateAndAddFinanceRequest(context, referenceNo, Models.RequestStatus.PendingApproval,
                    requestorId, title, amount);

                await context.SaveChangesAsync();

                requestId = financeRequest.Id;
            }

            var client = GetHttpClient(factory, role);

            var content = await GenerateContentAsync(client, tokenUri,
                new Dictionary<string, string>
                {
                    ["Id"] = requestId.ToString(),
                    ["Comment"] = approvalComment
                });

            // Act
            var response = await client.PostAsync(postUri, content);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var reloadedRequest = await assertContext.FinanceRequests.SingleAsync(x => x.Id == requestId);

            Assert.Equal(RequestStatus.Approved, reloadedRequest.Status);
        }

        [Fact]
        public async Task Approve_RequestWithChangedStatus_ShouldRedirectWithWarning()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string approverId = "test-user";
            const string approverEmail = "approver@test.com";
            const string requestorId = "requestor-user";
            const string requestorEmail = "requestor@test.com";

            const string referenceNo = "FR-TEST-001";
            const string title = "Office Equipment Purchase";
            const decimal amount = 75000m;

            const string role = "Approver";
            const string tokenUri = "/Approvals";
            const string postUri = "/Approvals/Approve";
            const string approvalComment = "Approved";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, approverId, approverEmail);
                CreateAndAddUser(context, requestorId, requestorEmail);

                var financeRequest = CreateAndAddFinanceRequest(
                    context,
                    referenceNo,
                    RequestStatus.PendingApproval,
                    requestorId,
                    title,
                    amount);

                await context.SaveChangesAsync();

                requestId = financeRequest.Id;
            }

            var client = GetHttpClient(factory, role);

            // User opens /Approvals while request is still PendingApproval
            var content = await GenerateContentAsync(
                client,
                tokenUri,
                new Dictionary<string, string>
                {
                    ["Id"] = requestId.ToString(),
                    ["Comment"] = approvalComment
                });

            // Simulate another tab/user changing the status before this POST
            using (var updateScope = factory.Services.CreateScope())
            {
                var context = updateScope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var request = await context.FinanceRequests
                    .SingleAsync(x => x.Id == requestId);

                request.Status = RequestStatus.Approved;

                await context.SaveChangesAsync();
            }

            // Act
            var response = await client.PostAsync(postUri, content);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var indexResponse = await client.GetAsync("/Approvals");
            indexResponse.EnsureSuccessStatusCode();

            var html = await indexResponse.Content.ReadAsStringAsync();

            Assert.Contains(
                "The request could not be approved. It may have been modified or is no longer available.",
                html);

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var reloadedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            Assert.Equal(RequestStatus.Approved, reloadedRequest.Status);
        }
    }
}
