namespace FinanceFlow.Services
{
    public interface ITimeZoneService
    {
        DateTime ConvertFromUtc(DateTime utcDateTime);
        string FormatFromUtc(DateTime utcDateTime);
    }
}
