namespace FinanceFlow.Client.Models;

public sealed class FinanceRequestDto
{
    public int Id { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}