using FinanceFlow.Configuration;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Services
{
    public class TimeZoneService(IOptions<AppOptions> options) : ITimeZoneService
    {
        private readonly AppOptions _options = options.Value;
        private readonly TimeZoneInfo _timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

        public DateTime ConvertFromUtc(DateTime utcDateTime)
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);
        }

        public string FormatFromUtc(DateTime utcDateTime)
        {
            return ConvertFromUtc(utcDateTime).ToString(_options.DateTimeFormat);
        }
    }
}