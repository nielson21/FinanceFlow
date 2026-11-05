using Microsoft.AspNetCore.Identity;

namespace FinanceFlow.Data
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
    }
}