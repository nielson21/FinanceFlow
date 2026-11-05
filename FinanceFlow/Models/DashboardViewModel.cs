namespace FinanceFlow.Models
{
    public class DashboardViewModel
    {
        public int DraftCount { get; set; }

        public int PendingApprovalCount { get; set; }

        public int ApprovedCount { get; set; }

        public int RejectedCount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal ApprovedAmount { get; set; }

        public decimal PendingApprovalAmount { get; set; }

        public List<RecentFinanceRequestViewModel> RecentRequests { get; set; } = [];
    }
}
