namespace FinanceFlow.Client.Models;

public sealed record ApiTokenResponse(
    string TokenType,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken);