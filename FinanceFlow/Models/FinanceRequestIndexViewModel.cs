namespace FinanceFlow.Models
{
    public class FinanceRequestIndexViewModel
    {
        public List<FinanceRequestListItemViewModel> Requests { get; set; } = [];

        public RequestStatus? Status { get; set; }

        public string? Search { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
