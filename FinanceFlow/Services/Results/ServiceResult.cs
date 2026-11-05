namespace FinanceFlow.Services.Results
{
    public class ServiceResult(bool isSuccess, string? referenceNo)
    {
        public bool IsSuccess { get; set; } = isSuccess;
        public string? ReferenceNo { get; set; } = referenceNo;
    }
}
