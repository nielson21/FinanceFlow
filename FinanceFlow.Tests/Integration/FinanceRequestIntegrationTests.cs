using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration;

public class FinanceRequestIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
{
    [Fact]
    public async Task FinanceRequests_ShouldDisplayCurrentUsersRequest()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new ApplicationUser
        {
            Id = "test-user",
            UserName = "test@test.com",
            Email = "test@test.com"
        });

        context.FinanceRequests.Add(new FinanceRequest
        {
            ReferenceNo = "FR-TEST-001",
            Title = "Test Integration Request",
            Amount = 1500,
            Status = RequestStatus.Draft,
            RequestorId = "test-user"
        });

        await context.SaveChangesAsync();

        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Requestor");

        // Act
        var response = await client.GetAsync("/FinanceRequests");

        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        Assert.Contains(
            "Test Integration Request",
            html);
    }

    [Fact]
    public async Task FinanceRequests_ShouldNotDisplayAnotherUsersRequest()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new ApplicationUser
        {
            Id = "another-user",
            UserName = "another@test.com",
            Email = "another@test.com"
        });

        context.FinanceRequests.Add(new FinanceRequest
        {
            ReferenceNo = "FR-TEST-OTHER",
            Title = "Another Users Secret Request",
            Amount = 9999,
            Status = RequestStatus.Draft,
            RequestorId = "another-user"
        });

        await context.SaveChangesAsync();

        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Requestor");

        // Act
        var response =
            await client.GetAsync("/FinanceRequests");

        var html =
            await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();

        Assert.DoesNotContain(
            "Another Users Secret Request",
            html);
    }

    [Fact]
    public async Task FinanceRequest_WithInvalidRequestor_ShouldFail()
    {
        await ResetDatabaseAsync(factory.Services);

        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.FinanceRequests.Add(new FinanceRequest
        {
            ReferenceNo = "FR-TEST-INVALID",
            Title = "Invalid Requestor Test",
            Amount = 1000,
            Status = RequestStatus.Draft,
            RequestorId = "user-does-not-exist"
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FinanceRequest_WithDuplicateReferenceNo_ShouldFail()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new ApplicationUser
        {
            Id = "test-user",
            UserName = "test@test.com",
            Email = "test@test.com"
        });

        context.FinanceRequests.Add(new FinanceRequest
        {
            ReferenceNo = "FR-DUPLICATE-001",
            Title = "Duplicate ReferenceNo Test",
            Amount = 1000,
            Status = RequestStatus.Draft,
            RequestorId = "test-user"
        });

        await context.SaveChangesAsync();

        context.FinanceRequests.Add(new FinanceRequest
        {
            ReferenceNo = "FR-DUPLICATE-001",
            Title = "This is the duplicate ReferenceNo",
            Amount = 2000,
            Status = RequestStatus.Draft,
            RequestorId = "test-user"
        });

        // Act & Assert

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }


    [Fact]
    public async Task FinanceRequest_DeleteActionUserWithHistory_ShouldFail()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new ApplicationUser
        {
            Id = "requestor-user",
            UserName = "requestor@test.com",
            Email = "requestor@test.com"
        });

        context.Users.Add(new ApplicationUser
        {
            Id = "approver-user",
            UserName = "approver@test.com",
            Email = "approver@test.com"
        });

        var request = new FinanceRequest
        {
            ReferenceNo = "FR-TEST-DELETE-USER",
            Title = "Delete User Test",
            Amount = 1000,
            Status = RequestStatus.Approved,
            RequestorId = "requestor-user"
        };

        context.FinanceRequests.Add(request);

        context.ApprovalHistories.Add(new ApprovalHistory
        {
            FinanceRequest = request,
            FromStatus = RequestStatus.PendingApproval,
            ToStatus = RequestStatus.Approved,
            ActionByUserId = "approver-user"
        });

        await context.SaveChangesAsync();

        var approverUser = await context.Users.SingleAsync(x => x.Id == "approver-user");

        // Act
        context.Users.Remove(approverUser);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FinanceRequest_DeleteRequest_ShouldDeleteApprovalHistory()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        context.Users.Add(new ApplicationUser
        {
            Id = "requestor-user",
            UserName = "requestor@test.com",
            Email = "requestor@test.com"
        });

        context.Users.Add(new ApplicationUser
        {
            Id = "approver-user",
            UserName = "approver@test.com",
            Email = "approver@test.com"
        });

        var request = new FinanceRequest
        {
            ReferenceNo = "FR-TEST-DELETE-CASCADE",
            Title = "Delete Cascade Test",
            Amount = 1000,
            Status = RequestStatus.Approved,
            RequestorId = "requestor-user"
        };

        context.FinanceRequests.Add(request);

        context.ApprovalHistories.Add(new ApprovalHistory
        {
            FinanceRequest = request,
            FromStatus = RequestStatus.PendingApproval,
            ToStatus = RequestStatus.Approved,
            ActionByUserId = "approver-user"
        });

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        // Act
        var requestToDelete = await context.FinanceRequests.SingleAsync(x => x.Id == request.Id);

        context.FinanceRequests.Remove(requestToDelete);

        await context.SaveChangesAsync();

        // Assert
        Assert.False(await context.ApprovalHistories.AnyAsync(x => x.FinanceRequestId == request.Id));
    }

    [Fact]
    public async Task FinanceRequest_TransactionRollback_ShouldNotSaveRequest()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateUser("test-user", "requestor@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await using var transaction = await context.Database.BeginTransactionAsync();

        var financeRequest = CreateFinanceRequest("FR-ROLLBACK-001", RequestStatus.Draft, "test-user");
        context.FinanceRequests.Add(financeRequest);
        await context.SaveChangesAsync();

        // Act
        await transaction.RollbackAsync();
        context.ChangeTracker.Clear();

        // Assert
        Assert.False(await context.FinanceRequests.AnyAsync(x => x.ReferenceNo == "FR-ROLLBACK-001"));
    }

    [Fact]
    public async Task FinanceRequest_TransactionCommit_ShouldSaveRequest()
    {
        await ResetDatabaseAsync(factory.Services);

        // Arrange
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateUser("test-user", "requestor@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await using var transaction = await context.Database.BeginTransactionAsync();

        var financeRequest = CreateFinanceRequest("FR-COMMIT-001", RequestStatus.Draft, "test-user");
        context.FinanceRequests.Add(financeRequest);
        await context.SaveChangesAsync();

        // Act
        await transaction.CommitAsync();
        context.ChangeTracker.Clear();

        // Assert
        Assert.True(await context.FinanceRequests.AnyAsync(x => x.ReferenceNo == "FR-COMMIT-001"));
    }

    [Fact]
    public async Task Create_InvalidAmount_ShouldNotCreateRequest()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string role = "Requestor";
        const string tokenUri = "/FinanceRequests/Create";
        const string postUri = "/FinanceRequests/Create";

        var client = GetHttpClient(factory, role);

        var content = await GenerateContentAsync(
            client,
            tokenUri,
            new Dictionary<string, string>
            {
                ["Title"] = "Test Request",
                ["Amount"] = "0"
            });

        // Act
        var response = await client.PostAsync(postUri, content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Amount must be greater than 0.",
            html);

        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.Empty(context.FinanceRequests);
    }

    [Theory]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.PendingApproval)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Index_FilterByStatus_ShouldReturnOnlyMatchingRequests(RequestStatus requestStatus)
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context,
                requestorId,
                requestorEmail);

            foreach (var item in Enum.GetValues<RequestStatus>())
            {
                CreateAndAddFinanceRequest(
                context,
                $"FR-{item.ToString().ToUpper()}-001",
                item,
                requestorId,
                $"{item} Request",
                1000m);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync(
            $"/FinanceRequests?status={requestStatus}");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        foreach (var item in Enum.GetValues<RequestStatus>())
        {
            if (item == requestStatus)
            {
                Assert.Contains($"{item} Request", html);
            }
            else
            {
                Assert.DoesNotContain($"{item} Request", html);
            }
        }
    }

    [Theory]
    [InlineData("Laptop", "FR-SEARCH-LAPTOP", "FR-SEARCH-OFFICE")]
    [InlineData("OFFICE", "FR-SEARCH-OFFICE", "FR-SEARCH-LAPTOP")]
    public async Task Index_Search_ShouldReturnOnlyMatchingRequests(
    string search,
    string expectedReferenceNo,
    string unexpectedReferenceNo)
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

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
                "FR-SEARCH-LAPTOP",
                RequestStatus.Draft,
                requestorId,
                "Laptop Repair",
                5000m);

            CreateAndAddFinanceRequest(
                context,
                "FR-SEARCH-OFFICE",
                RequestStatus.Draft,
                requestorId,
                "Office Supplies",
                3000m);

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync(
            $"/FinanceRequests?search={Uri.EscapeDataString(search)}");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(expectedReferenceNo, html);
        Assert.DoesNotContain(unexpectedReferenceNo, html);
    }

    [Fact]
    public async Task Index_Pagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(
                context,
                requestorId,
                requestorEmail);

            var baseDate = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 1; i <= 6; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-PAGE-{i:000}",
                    RequestStatus.Draft,
                    requestorId,
                    $"Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act - Page 1
        var page1Response = await client.GetAsync(
            "/FinanceRequests?page=1");

        page1Response.EnsureSuccessStatusCode();

        var page1Html = await page1Response.Content.ReadAsStringAsync();

        // Assert - newest 5 records should be on page 1
        Assert.DoesNotContain("FR-PAGE-001", page1Html);

        for (var i = 2; i <= 6; i++)
        {
            Assert.Contains($"FR-PAGE-{i:000}", page1Html);
        }

        // Act - Page 2
        var page2Response = await client.GetAsync(
            "/FinanceRequests?page=2");

        page2Response.EnsureSuccessStatusCode();

        var page2Html = await page2Response.Content.ReadAsStringAsync();

        // Assert - oldest remaining record should be on page 2
        Assert.Contains("FR-PAGE-001", page2Html);

        for (var i = 2; i <= 6; i++)
        {
            Assert.DoesNotContain($"FR-PAGE-{i:000}", page2Html);
        }
    }

    [Fact]
    public async Task Index_PaginationWithStatusFilter_ShouldReturnCorrectPage()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(
                context,
                requestorId,
                requestorEmail);

            var baseDate = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 1; i <= 6; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-PAGE-{i:000}",
                    RequestStatus.Approved,
                    requestorId,
                    $"Approved Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            for (var i = 7; i <= 8; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-PAGE-{i:000}",
                    RequestStatus.Draft,
                    requestorId,
                    $"Draft Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var page2Response = await client.GetAsync(
            "/FinanceRequests?status=Approved&page=2");

        page2Response.EnsureSuccessStatusCode();

        var page2Html = await page2Response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("FR-PAGE-001", page2Html);
        Assert.DoesNotContain("Draft Request", page2Html);

        for (var i = 2; i <= 6; i++)
        {
            Assert.DoesNotContain($"FR-PAGE-{i:000}", page2Html);
        }
    }

    [Fact]
    public async Task Index_PaginationWithSearch_ShouldReturnCorrectPage()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(
                context,
                requestorId,
                requestorEmail);

            var baseDate = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 1; i <= 6; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-SEARCH-{i:000}",
                    RequestStatus.PendingApproval,
                    requestorId,
                    $"Office Equipment Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            for (var i = 7; i <= 8; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-SEARCH-{i:000}",
                    RequestStatus.PendingApproval,
                    requestorId,
                    $"Laptop Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var page2Response = await client.GetAsync(
            "/FinanceRequests?search=Office&page=2");

        page2Response.EnsureSuccessStatusCode();

        var page2Html = await page2Response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("FR-SEARCH-001", page2Html);
        Assert.DoesNotContain("Laptop Request", page2Html);

        for (var i = 2; i <= 6; i++)
        {
            Assert.DoesNotContain($"FR-SEARCH-{i:000}", page2Html);
        }
    }

    [Theory]
    [InlineData(0, "FR-PAGE-006")]     // page 0 -> page 1
    [InlineData(-1, "FR-PAGE-006")]    // negative -> page 1
    [InlineData(999, "FR-PAGE-001")]   // too high -> last page
    public async Task Index_InvalidPage_ShouldUseValidPage(
    int page,
    string expectedReferenceNo)
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(
                context,
                requestorId,
                requestorEmail);

            var baseDate = new DateTime(
                2026, 10, 22, 0, 0, 0,
                DateTimeKind.Utc);

            for (var i = 1; i <= 6; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-PAGE-{i:000}",
                    RequestStatus.Draft,
                    requestorId,
                    $"Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync(
            $"/FinanceRequests?page={page}");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(expectedReferenceNo, html);
    }

    [Fact]
    public async Task Edit_Draft_ShouldUpdateRequest()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, requestorEmail);

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-EDIT-001",
                RequestStatus.Draft,
                requestorId,
                "Original Title",
                1000m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        var client = GetHttpClient(factory, role);

        var content = await GenerateContentAsync(
            client,
            $"/FinanceRequests/Edit/{requestId}",
            new Dictionary<string, string>
            {
                ["Id"] = requestId.ToString(),
                ["Title"] = "Updated Title",
                ["Amount"] = "2500"
            });

        // Act
        var response = await client.PostAsync(
            $"/FinanceRequests/Edit/{requestId}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var assertScope = factory.Services.CreateScope();

        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updatedRequest = await assertContext.FinanceRequests.SingleAsync(x => x.Id == requestId);

        Assert.Equal("Updated Title", updatedRequest.Title);
        Assert.Equal(2500m, updatedRequest.Amount);
    }

    [Fact]
    public async Task Edit_NonDraft_ShouldNotUpdateRequest()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, requestorEmail);

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-EDIT-002",
                RequestStatus.Approved,
                requestorId,
                "Original Title",
                1000m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        var client = GetHttpClient(factory, role);

        var token = await GetHtmlValueAsync(
            client,
            "/FinanceRequests/Create");

        var content = GenerateContent(
            token,
            new Dictionary<string, string>
            {
                ["Id"] = requestId.ToString(),
                ["Title"] = "Updated Title",
                ["Amount"] = "2500"
            });

        // Act
        var response = await client.PostAsync(
            $"/FinanceRequests/Edit/{requestId}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var assertScope = factory.Services.CreateScope();

        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var reloadedRequest = await assertContext.FinanceRequests
            .SingleAsync(x => x.Id == requestId);

        Assert.Equal("Original Title", reloadedRequest.Title);
        Assert.Equal(1000m, reloadedRequest.Amount);
        Assert.Equal(RequestStatus.Approved, reloadedRequest.Status);
    }

    [Fact]
    public async Task Details_OwnRequest_ShouldReturnRequest()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, requestorEmail);

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-DETAILS-001",
                RequestStatus.Draft,
                requestorId,
                "Details Test Request",
                1500m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync($"/FinanceRequests/Details/{requestId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Details Test Request", html);
        Assert.Contains("1,500.00", html);
        Assert.Contains("Draft", html);
    }

    [Fact]
    public async Task Details_OtherUsersRequest_ShouldReturnNotFound()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string currentUserId = "test-user";
        const string currentUserEmail = "requestor@test.com";
        const string otherUserId = "other-user";
        const string otherUserEmail = "other@test.com";
        const string role = "Requestor";

        int requestId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, currentUserId, currentUserEmail);
            CreateAndAddUser(context, otherUserId, otherUserEmail);

            var request = CreateAndAddFinanceRequest(
                context,
                "FR-DETAILS-002",
                RequestStatus.Draft,
                otherUserId,
                "Other User Request",
                2000m);

            await context.SaveChangesAsync();

            requestId = request.Id;
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync($"/FinanceRequests/Details/{requestId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}