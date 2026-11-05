using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

public class DashboardIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
{
    [Fact]
    public async Task Dashboard_ShouldShowCorrectSummaryForCurrentUser()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string otherUserId = "other-user";
        const string otherUserEmail = "other@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, requestorEmail);
            CreateAndAddUser(context, otherUserId, otherUserEmail);

            for (var i = 1; i <= 1; i++)
            {
                CreateAndAddFinanceRequest(
                    context,
                    $"FR-DRAFT-{i:000}",
                    RequestStatus.Draft,
                    requestorId,
                    $"Draft Request {i}",
                    1000m);
            }

            for (var i = 1; i <= 2; i++)
            {
                CreateAndAddFinanceRequest(
                    context,
                    $"FR-PENDING-{i:000}",
                    RequestStatus.PendingApproval,
                    requestorId,
                    $"Pending Request {i}",
                    2000m);
            }

            for (var i = 1; i <= 3; i++)
            {
                CreateAndAddFinanceRequest(
                    context,
                    $"FR-APPROVED-{i:000}",
                    RequestStatus.Approved,
                    requestorId,
                    $"Approved Request {i}",
                    3000m);
            }

            for (var i = 1; i <= 4; i++)
            {
                CreateAndAddFinanceRequest(
                    context,
                    $"FR-REJECTED-{i:000}",
                    RequestStatus.Rejected,
                    requestorId,
                    $"Rejected Request {i}",
                    4000m);
            }

            // Should NOT be included in current user's dashboard
            CreateAndAddFinanceRequest(
                context,
                "FR-OTHER-001",
                RequestStatus.Approved,
                otherUserId,
                "Other User Request",
                50000m);

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync("/Dashboard");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("Draft", html);
        Assert.Contains("Pending Approval", html);
        Assert.Contains("Approved", html);
        Assert.Contains("Rejected", html);

        Assert.Contains(">1<", html);
        Assert.Contains(">2<", html);
        Assert.Contains(">3<", html);
        Assert.Contains(">4<", html);

        Assert.Contains("data-testid=\"total-amount\">₱30,000.00</div>", decodedHtml);
        Assert.Contains("data-testid=\"pending-amount\">₱4,000.00</div>", decodedHtml);
        Assert.Contains("data-testid=\"approved-amount\">₱9,000.00</div>", decodedHtml);

        Assert.DoesNotContain("80,000.00", decodedHtml);
    }

    [Fact]
    public async Task Dashboard_ShouldShowLatestFiveRequestsOnly()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string requestorId = "test-user";
        const string requestorEmail = "requestor@test.com";
        const string role = "Requestor";

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            CreateAndAddUser(context, requestorId, requestorEmail);

            var baseDate = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 1; i <= 6; i++)
            {
                var request = CreateAndAddFinanceRequest(
                    context,
                    $"FR-RECENT-{i:000}",
                    RequestStatus.Draft,
                    requestorId,
                    $"Recent Request {i}",
                    1000m);

                request.CreatedAt = baseDate.AddMinutes(i);
            }

            await context.SaveChangesAsync();
        }

        var client = GetHttpClient(factory, role);

        // Act
        var response = await client.GetAsync("/Dashboard");

        // Assert
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("FR-RECENT-001", html);

        for (var i = 2; i <= 6; i++)
        {
            Assert.Contains($"FR-RECENT-{i:000}", html);
        }

        var request6Index = html.IndexOf("FR-RECENT-006", StringComparison.Ordinal);
        var request5Index = html.IndexOf("FR-RECENT-005", StringComparison.Ordinal);
        var request4Index = html.IndexOf("FR-RECENT-004", StringComparison.Ordinal);
        var request3Index = html.IndexOf("FR-RECENT-003", StringComparison.Ordinal);
        var request2Index = html.IndexOf("FR-RECENT-002", StringComparison.Ordinal);

        Assert.True(request6Index < request5Index);
        Assert.True(request5Index < request4Index);
        Assert.True(request4Index < request3Index);
        Assert.True(request3Index < request2Index);
    }
}