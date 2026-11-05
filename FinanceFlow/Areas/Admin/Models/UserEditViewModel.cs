namespace FinanceFlow.Areas.Admin.Models
{
    public class UserEditViewModel
    {
        public string Id { get; set; } = null!;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}