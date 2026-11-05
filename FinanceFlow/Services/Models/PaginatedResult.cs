namespace FinanceFlow.Services.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = [];

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }
    }
}