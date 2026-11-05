using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Models
{
    public class ApprovalDecisionViewModel
    {
        public int Id { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }
    }
}