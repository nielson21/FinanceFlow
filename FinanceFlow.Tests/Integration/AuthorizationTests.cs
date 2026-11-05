using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinanceFlow.Tests.Integration;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FinanceRequests_WhenNotLoggedIn_ShouldRedirectToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act
        var response = await client.GetAsync("/FinanceRequests");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.Contains(
            "/Identity/Account/Login",
            response.Headers.Location?.ToString());
    }
}