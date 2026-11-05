using Microsoft.AspNetCore.Authorization;

namespace FinanceFlow.Authorization
{
    public class LargeAmountApprovalRequirement (decimal threshold) : IAuthorizationRequirement
    {
        public decimal Threshold { get; } = threshold;
    }
}
