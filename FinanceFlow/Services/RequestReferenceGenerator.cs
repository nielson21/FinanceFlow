namespace FinanceFlow.Services
{
    public class RequestReferenceGenerator : IRequestReferenceGenerator
    {
        private readonly TimeProvider _timeProvider;

        public RequestReferenceGenerator(TimeProvider timeProvider)
        {
                _timeProvider = timeProvider;
        }
        public string Generate()
        {
            var year = _timeProvider.GetUtcNow().Year;
            var uniquePart = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpper();

            return $"FR-{year}-{uniquePart}";
        }
    }
}
