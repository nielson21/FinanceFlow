using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Models
{
    public class FinanceRequestEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}