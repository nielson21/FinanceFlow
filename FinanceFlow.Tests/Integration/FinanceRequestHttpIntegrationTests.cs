using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;


namespace FinanceFlow.Tests.Integration
{
    public class FinanceRequestHttpIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        private readonly FinanceFlowWebApplicationFactory _factory = factory;

        [Fact]
        public async Task Submit_WithoutAntiForgeryToken_ShouldReturnBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            client.DefaultRequestHeaders.Add("X-Test-Role", "Requestor");

            // Act
            var response = await client.PostAsync("/FinanceRequests/Submit/999", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Submit_WithValidAntiForgeryToken_ShouldSubmitRequest()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var referenceNo = "FR-TEST-SUBMIT";

            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);

            var request = CreateFinanceRequest(referenceNo, RequestStatus.Draft, requestorId);
            context.FinanceRequests.Add(request);

            await context.SaveChangesAsync();

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            client.DefaultRequestHeaders.Add("X-Test-Role", "Requestor");

            string token = await GetHtmlValueAsync(client, "/FinanceRequests");
            var content = GenerateContent(token);

            // Act
            var postResponse = await client.PostAsync($"/FinanceRequests/Submit/{request.Id}", content);
            Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

            context.ChangeTracker.Clear();

            var reloadedRequest = await context.FinanceRequests.SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.Equal(RequestStatus.PendingApproval, reloadedRequest.Status);
        }

        [Fact]
        public async Task Submit_NonDraftRequest_ShouldNotChangeStatus()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "test-user";
            var requestorEmail = "requestor@test.com";
            var referenceNo = "FR-TEST-SUBMIT";
            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);
            var request = CreateFinanceRequest(referenceNo, RequestStatus.PendingApproval, requestorId);
            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Requestor");

            string token = await GetHtmlValueAsync(client, "/FinanceRequests/Create");
            var content = GenerateContent(token);

            // Act
            var postResponse = await client.PostAsync($"/FinanceRequests/Submit/{request.Id}", content);
            Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);

            context.ChangeTracker.Clear();

            var reloadedRequest = await context.FinanceRequests.SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.Equal(RequestStatus.PendingApproval, reloadedRequest.Status);
            Assert.False(await context.ApprovalHistories.AnyAsync(x => x.FinanceRequestId == reloadedRequest.Id));
        }

        [Fact]
        public async Task Approve_WithValidAntiForgeryToken_ShouldApproveRequest()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "requestor-user";
            var requestorEmail = "requestor@test.com";
            var approverId = "test-user";
            var approverEmail = "approver@test.com";
            var referenceNo = "FR-TEST-APPROVE";
            var comment = "Approval test";
            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);
            var approver = CreateUser(approverId, approverEmail);
            context.Users.Add(approver);
            var request = CreateFinanceRequest(referenceNo, RequestStatus.PendingApproval, requestorId);
            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Approver");

            string token = await GetHtmlValueAsync(client, "/Approvals");
            var content = GenerateContent(token,
                new Dictionary<string, string>
                {
                    ["Id"] = request.Id.ToString(),
                    ["Comment"] = comment
                });

            // Act
            var postResponse = await client.PostAsync("/Approvals/Approve", content);
            Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

            context.ChangeTracker.Clear();

            var reloadedRequest = await context.FinanceRequests.Include(x => x.ApprovalHistories).SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.Equal(RequestStatus.Approved, reloadedRequest.Status);
            var history = Assert.Single(reloadedRequest.ApprovalHistories);
            Assert.Equal(RequestStatus.PendingApproval, history.FromStatus);
            Assert.Equal(RequestStatus.Approved, history.ToStatus);
            Assert.Equal(approverId, history.ActionByUserId);
            Assert.Equal(comment, history.Comment);
        }

        [Fact]
        public async Task Reject_WithValidAntiForgeryToken_ShouldRejectRequest()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var requestorId = "requestor-user";
            var requestorEmail = "requestor@test.com";
            var approverId = "test-user";
            var approverEmail = "approver@test.com";
            var referenceNo = "FR-TEST-REJECT";
            var comment = "Rejection test";
            var requestor = CreateUser(requestorId, requestorEmail);
            context.Users.Add(requestor);
            var approver = CreateUser(approverId, approverEmail);
            context.Users.Add(approver);
            var request = CreateFinanceRequest(referenceNo, RequestStatus.PendingApproval, requestorId);
            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Approver");

            string token = await GetHtmlValueAsync(client, "/Approvals");
            var content = GenerateContent(token,
                new Dictionary<string, string>
                {
                    ["Id"] = request.Id.ToString(),
                    ["Comment"] = comment
                });

            // Act
            var postResponse = await client.PostAsync("/Approvals/Reject", content);

            context.ChangeTracker.Clear();

            var reloadedRequest = await context.FinanceRequests.Include(x => x.ApprovalHistories).SingleAsync(x => x.ReferenceNo == referenceNo);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
            Assert.Equal(RequestStatus.Rejected, reloadedRequest.Status);
            var history = Assert.Single(reloadedRequest.ApprovalHistories);
            Assert.Equal(RequestStatus.PendingApproval, history.FromStatus);
            Assert.Equal(RequestStatus.Rejected, history.ToStatus);
            Assert.Equal(approverId, history.ActionByUserId);
            Assert.Equal(comment, history.Comment);
        }

        [Fact]
        public async Task Approve_AsRequestor_ShouldReturnForbidden()
        {
            // Arrange
            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Requestor");

            // Act
            var response = await client.PostAsync("/Approvals/Approve", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Submit_AsApprover_ShouldReturnForbidden()
        {
            // Arrange
            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Approver");

            // Act
            var response = await client.PostAsync("/FinanceRequests/Submit/999", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Reject_AsRequestor_ShouldReturnForbidden()
        {
            // Arrange
            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Requestor");

            // Act
            var response = await client.PostAsync("/Approvals/Reject", null);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
