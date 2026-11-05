using FinanceFlow.Client.Exceptions;
using FinanceFlow.Client.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanceFlow.Client.Clients
{
    public sealed class FinanceFlowApiClient(HttpClient httpClient)
    {
        private ApiTokenResponse? _currentTokens;
        private DateTime _accessTokenExpiresAtUtc;

        public async Task<PagedResponseDto<FinanceRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
        {
            var accessToken = await GetValidAccessTokenAsync();
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, "api/finance-requests", accessToken);

            var response = await httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("API response was empty.");
        }

        public async Task LoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            var loginRequest = new ApiLoginRequest(email, password);

            using var response = await httpClient.PostAsJsonAsync(
                "api/auth/login",
                loginRequest,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<ApiTokenResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Login response was empty.");

            StoreTokens(token);
        }

        public async Task<FinanceRequestDto> CreateRequestAsync(CreateFinanceRequestRequest model, CancellationToken cancellationToken = default)
        {
            var accessToken = await GetValidAccessTokenAsync();

            using var request = CreateAuthenticatedRequest(HttpMethod.Post, "api/finance-requests", accessToken, model);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessfulResponseAsync(response, cancellationToken);

            return await response.Content.ReadFromJsonAsync<FinanceRequestDto>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("API response was empty.");
        }

        public async Task UpdateRequestAsync(int id, UpdateFinanceRequestRequest model, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

            var accessToken = await GetValidAccessTokenAsync();

            using var request = CreateAuthenticatedRequest(HttpMethod.Put, $"api/finance-requests/{id}", accessToken, model);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            await EnsureSuccessfulResponseAsync(response, cancellationToken);
        }

        public async Task<ApiTokenResponse> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

            using var response = await httpClient.PostAsJsonAsync(
                "api/auth/refresh",
                new { refreshToken },
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Logout();
                throw new InvalidOperationException("Refresh token was expired or invalid. Please log in again.");
            }

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<ApiTokenResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Refresh response was empty.");

            StoreTokens(token);

            return token;
        }

        public void Logout()
        {
            _currentTokens = null;
            _accessTokenExpiresAtUtc = DateTime.MinValue;
        }

        private async Task<string> GetValidAccessTokenAsync()
        {
            if (_currentTokens == null) throw new InvalidOperationException("No active session. Please log in first.");

            if (DateTime.UtcNow >= _accessTokenExpiresAtUtc.AddSeconds(-30))
            {
                await RefreshTokenAsync(_currentTokens.RefreshToken);
            }

            return _currentTokens.AccessToken;
        }

        private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string requestUri, string accessToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

            var request = new HttpRequestMessage(method, requestUri);

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

            return request;
        }

        private static HttpRequestMessage CreateAuthenticatedRequest<T>(HttpMethod method, string requestUri, string accessToken, T model)
        {
            var request = CreateAuthenticatedRequest(method, requestUri, accessToken);

            request.Content = JsonContent.Create(model);

            return request;
        }

        private static async Task EnsureSuccessfulResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var validationProblem = await response.Content
                    .ReadFromJsonAsync<ApiValidationProblem>(
                        cancellationToken: cancellationToken);

                if (validationProblem?.Errors is { Count: > 0 })
                {
                    throw new ApiValidationException(validationProblem.Errors);
                }
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);

                throw new HttpRequestException(error?.Message ?? "A conflict occurred.", null, response.StatusCode);
            }

            response.EnsureSuccessStatusCode();
        }

        private void StoreTokens(ApiTokenResponse tokens)
        {
            _currentTokens = tokens;

            _accessTokenExpiresAtUtc = DateTime
                .UtcNow
                .AddSeconds(tokens.ExpiresIn);
        }
    }
}