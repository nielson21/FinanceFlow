using System.Net;

namespace FinanceFlow.Tests.Integration;

public class RoleAuthorizationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
{
    private readonly FinanceFlowWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Requestor_ShouldAccessFinanceRequests()
    {
        var client = _factory.CreateClient(
            new()
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Requestor");

        var response =
            await client.GetAsync("/FinanceRequests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Requestor_ShouldNotAccessAdmin()
    {
        var client = _factory.CreateClient(
            new()
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Requestor");

        var response =
            await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_ShouldAccessAdmin()
    {
        var client = _factory.CreateClient(
            new()
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            "Admin");

        var response =
            await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}