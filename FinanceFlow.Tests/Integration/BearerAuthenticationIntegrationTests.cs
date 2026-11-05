using FinanceFlow.Api.Models;
using FinanceFlow.Data;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration;

public class BearerAuthenticationIntegrationTests(
    FinanceFlowRealAuthWebApplicationFactory factory)
    : IClassFixture<FinanceFlowRealAuthWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldAllowBearerAuthenticatedApiRequest()
    {
        // Arrange
        await ResetDatabaseAsync(factory.Services);

        const string email = "bearer@test.com";
        const string password = "TestPass123!";

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);

            Assert.True(result.Succeeded);
        }

        using var client = GetHttpClient(factory);

        // Verify access is denied without a token
        var unauthorizedResponse = await client.GetAsync(
            "/api/finance-requests");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            unauthorizedResponse.StatusCode);

        // Login
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new ApiLoginDto
            {
                Email = email,
                Password = password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var tokenResponse = await loginResponse.Content
            .ReadFromJsonAsync<AccessTokenResponse>();

        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.AccessToken));

        // Access protected API using the real bearer token
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance-requests");

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokenResponse.AccessToken);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}