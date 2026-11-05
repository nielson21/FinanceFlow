using System.ComponentModel.DataAnnotations;
using FinanceFlow.Models;

namespace FinanceFlow.Api.Models
{
    public class FinanceRequestQueryDto
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public RequestStatus? Status { get; set; }

        public string? Search { get; set; }
    }
}