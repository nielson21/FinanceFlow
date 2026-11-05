using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models
{
    public class CreateFinanceRequestDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string? Title { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99",
            ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }
    }
}
