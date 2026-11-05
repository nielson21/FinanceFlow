namespace FinanceFlow.Services.Models
{
    public class FinanceRequestDashboardSummary
    {
        public int DraftCount { get; set; }

        public int PendingApprovalCount { get; set; }

        public int ApprovedCount { get; set; }

        public int RejectedCount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal ApprovedAmount { get; set; }

        public decimal PendingApprovalAmount { get; set; }

        public List<RecentFinanceRequestSummary> RecentRequests { get; set; } = [];
    }
}
