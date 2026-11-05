namespace FinanceFlow.Client.Models;

public sealed class ApiValidationProblem
{
    public Dictionary<string, string[]> Errors { get; set; } = [];
}