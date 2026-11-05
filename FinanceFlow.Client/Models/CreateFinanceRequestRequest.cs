namespace FinanceFlow.Client.Models;

public sealed class CreateFinanceRequestRequest
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
