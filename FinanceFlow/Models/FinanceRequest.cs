using System.ComponentModel.DataAnnotations;
using FinanceFlow.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Models
{
    public class FinanceRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string RequestorId { get; set; } = string.Empty;

        public ApplicationUser Requestor { get; set; } = null!;

        public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

        [Required]
        [StringLength(30)]
        public string ReferenceNo { get; set; } = string.Empty;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}