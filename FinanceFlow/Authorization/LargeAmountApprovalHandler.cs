using FinanceFlow.Models;
using Microsoft.AspNetCore.Authorization;

namespace FinanceFlow.Authorization
{
    public class LargeAmountApprovalHandler : AuthorizationHandler<LargeAmountApprovalRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LargeAmountApprovalRequirement requirement)
        {
            if (context.Resource is not FinanceRequest request)
                return Task.CompletedTask;

            if (request.Amount <= requirement.Threshold)
            {
                context.Succeed(requirement);
            }
            else if (context.User.HasClaim("CanApproveLargeAmount", "true"))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
