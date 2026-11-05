namespace FinanceFlow.Areas.Admin.Models
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public bool IsActive { get; set; }

        public IList<string> Roles { get; set;  } = new List<string>();
    }
}
