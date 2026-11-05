using FinanceFlow.Data;
using Microsoft.AspNetCore.Identity;

namespace FinanceFlow.Models
{
    public class ApprovalHistory
    {
        public int Id { get; set; }

        public int FinanceRequestId { get; set; }
        public FinanceRequest FinanceRequest { get; set; } = null!;

        public RequestStatus FromStatus { get; set; }
        public RequestStatus ToStatus { get; set; }

        public string ActionByUserId { get; set; } = string.Empty;
        public ApplicationUser ActionByUser { get; set; } = null!;

        public string? Comment { get; set; }

        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    }
}