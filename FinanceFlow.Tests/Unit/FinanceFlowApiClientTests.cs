using System.Net;
using System.Net.Http.Json;
using FinanceFlow.Client.Clients;
using FinanceFlow.Client.Models;

namespace FinanceFlow.Tests.Unit;

public class FinanceFlowApiClientTests
{
    [Fact]
    public async Task GetRequestsAsync_ShouldSendBearerToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var apiClient = new FinanceFlowApiClient(httpClient);

        await apiClient.LoginAsync("test@example.com", "password123");
        // Act
        await apiClient.GetRequestsAsync();

        // Assert
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-access-token", handler.AuthorizationParameter);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post &&
                request.RequestUri?.AbsolutePath == "/api/auth/login")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        AccessToken = "test-access-token",
                        RefreshToken = "test-refresh-token",
                        ExpiresIn = 3600
                    })
                });
            }

            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(
                    new PagedResponseDto<FinanceRequestDto>())
            };

            return Task.FromResult(response);
        }
    }
}