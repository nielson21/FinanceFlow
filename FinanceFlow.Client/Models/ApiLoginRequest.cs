namespace FinanceFlow.Client.Models;

public sealed class ApiLoginRequest(string email, string password)
{
    public string Email { get; } = email;
    public string Password { get; } = password;
}