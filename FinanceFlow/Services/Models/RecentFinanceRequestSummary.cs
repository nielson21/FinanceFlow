using FinanceFlow.Models;

namespace FinanceFlow.Services.Models
{
    public class RecentFinanceRequestSummary
    {
        public int Id { get; set; }

        public string ReferenceNo { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
