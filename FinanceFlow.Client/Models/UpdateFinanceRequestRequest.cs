namespace FinanceFlow.Client.Models;

public sealed class UpdateFinanceRequestRequest
{
    public UpdateFinanceRequestRequest(FinanceRequestDto? request = null)
    {
        if (request is not null)
        {
            Title = request.Title;
            Amount = request.Amount;
            RowVersion = request.RowVersion;
        }
    }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public byte[] RowVersion { get; set; } = [];
}