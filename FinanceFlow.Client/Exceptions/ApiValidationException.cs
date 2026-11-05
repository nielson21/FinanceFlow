namespace FinanceFlow.Client.Exceptions;

public sealed class ApiValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ApiValidationException(Dictionary<string, string[]> errors)
        : base("API validation failed.")
    {
        Errors = errors;
    }
}