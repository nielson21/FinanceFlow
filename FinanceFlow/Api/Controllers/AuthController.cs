using FinanceFlow.Api.Models;
using FinanceFlow.Data;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(ApiLoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email!);

            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

            var result = await signInManager.PasswordSignInAsync(
                user,
                dto.Password!,
                isPersistent: false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            return new EmptyResult();
        }

        [HttpGet("me")]
        [Authorize(Policy = "ApiBearer")]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserId = userManager.GetUserId(User)
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(
            RefreshRequest model,
            [FromServices] IOptionsMonitor<BearerTokenOptions> bearerOptions,
            [FromServices] TimeProvider timeProvider)
        {
            var protector = bearerOptions
                .Get(IdentityConstants.BearerScheme)
                .RefreshTokenProtector;

            var ticket = protector.Unprotect(model.RefreshToken);

            if (ticket?.Properties.ExpiresUtc is not { } expiresUtc ||
                timeProvider.GetUtcNow() >= expiresUtc)
            {
                return Unauthorized();
            }

            var user = await signInManager.ValidateSecurityStampAsync(ticket.Principal);

            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var principal = await signInManager.CreateUserPrincipalAsync(user);

            return SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
        }
    }
}
