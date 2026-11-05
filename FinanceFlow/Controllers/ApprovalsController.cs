using FinanceFlow.Constants;
using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Controllers
{
    [Authorize(Roles = UserRoles.Approver)]
    public class ApprovalsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFinanceRequestService financeRequestService, IAuthorizationService authorizationService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IFinanceRequestService _financeRequestService = financeRequestService;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.FinanceRequests
                .Include(x => x.Requestor)
                .Where(x => x.Status == RequestStatus.PendingApproval)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(ApprovalDecisionViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var financeRequest = await _financeRequestService.GetByIdAsync(model.Id);

            if (financeRequest == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, financeRequest, "LargeAmountApproval");

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            var result = await _financeRequestService.ApproveAsync(model.Id, userId, model.Comment);

            if (!result.IsSuccess)
                TempData[TempDataKeys.ErrorMessage] = "The request could not be approved. It may have been modified or is no longer available.";
            else
                TempData[TempDataKeys.SuccessMessage] = $"Finance request with reference no. {result.ReferenceNo} approved successfully!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(ApprovalDecisionViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var result = await _financeRequestService.RejectAsync(model.Id, userId, model.Comment);

            if (!result.IsSuccess)
                TempData[TempDataKeys.ErrorMessage] = "The request could not be rejected. It may have been modified or is no longer available.";
            else
                TempData[TempDataKeys.SuccessMessage] = $"Finance request with reference no. {result.ReferenceNo} rejected successfully!";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanApproveLargeAmount")]
        public IActionResult LargeAmount()
        {
            return View();
        }
    }
}
