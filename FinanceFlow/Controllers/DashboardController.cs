using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.Controllers
{
    [Authorize]
    public class DashboardController(IFinanceRequestService financeRequestService, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var summary = await financeRequestService.GetDashboardSummaryAsync(userId);

            var model = new DashboardViewModel
            {
                DraftCount = summary.DraftCount,
                PendingApprovalCount = summary.PendingApprovalCount,
                ApprovedCount = summary.ApprovedCount,
                RejectedCount = summary.RejectedCount,
                TotalAmount = summary.TotalAmount,
                PendingApprovalAmount = summary.PendingApprovalAmount,
                ApprovedAmount = summary.ApprovedAmount,
                RecentRequests = [.. summary.RecentRequests
                    .Select(x => new RecentFinanceRequestViewModel
                    {
                        Id = x.Id,
                        ReferenceNo = x.ReferenceNo,
                        Title = x.Title,
                        Amount = x.Amount,
                        Status = x.Status,
                        CreatedAt = x.CreatedAt
                    })]
            };

            return View(model);
        }
    }
}