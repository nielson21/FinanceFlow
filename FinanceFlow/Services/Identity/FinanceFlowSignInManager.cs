using FinanceFlow.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Services.Identity
{
    public sealed class FinanceFlowSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation) : SignInManager<ApplicationUser>(
            userManager,
            contextAccessor,
            claimsFactory,
            optionsAccessor,
            logger,
            schemes,
            confirmation)
    {
        public override async Task<bool> CanSignInAsync(ApplicationUser user)
        {
            if (!user.IsActive)
            {
                return false;
            }

            return await base.CanSignInAsync(user);
        }
    }
}