using System.ComponentModel.DataAnnotations;

namespace FinanceFlow.Api.Models;

public class ApiLoginDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}