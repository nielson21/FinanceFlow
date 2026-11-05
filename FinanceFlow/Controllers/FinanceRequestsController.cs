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
    [Authorize(Roles = UserRoles.Requestor)]
    public class FinanceRequestsController(UserManager<ApplicationUser> userManager,
        IFinanceRequestService financeRequestService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(RequestStatus? status, string? search, int page = 1)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            const int pageSize = 5;

            var result = await financeRequestService.GetPagedByRequestorAsync(userId, status, search, page, pageSize);

            var model = new FinanceRequestIndexViewModel
            {
                Requests = [.. result.Items.Select(x => new FinanceRequestListItemViewModel
                    {
                        Id = x.Id,
                        ReferenceNo = x.ReferenceNo,
                        Title = x.Title,
                        Amount = x.Amount,
                        Status = x.Status,
                        CreatedAt = x.CreatedAt
                    })],
                Status = status,
                Search = search,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinanceRequestCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            await financeRequestService.CreateAsync(model.Title, model.Amount, user.Id);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var request = await financeRequestService.GetEditableByRequestorAsync(id, userId);

            if (request == null)
            {
                return NotFound();
            }

            var model = new FinanceRequestEditViewModel
            {
                Id = request.Id,
                Title = request.Title,
                Amount = request.Amount,
                RowVersion = request.RowVersion
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FinanceRequestEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            try
            {
                var result = await financeRequestService.UpdateDraftAsync(id, userId, model.Title, model.Amount, model.RowVersion);

                if (!result.IsSuccess)
                {
                    return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty,
                    "This request has been modified by another user. Please reload the page to view the latest changes before editing again.");

                return View(model);
            }

            TempData[TempDataKeys.SuccessMessage] = "Finance request updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var result = await financeRequestService.SubmitAsync(id, userId);

            if (!result.IsSuccess)
            {
                return NotFound();
            }

            TempData[TempDataKeys.SuccessMessage] = $"Finance request with reference no. {result.ReferenceNo} submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var request = await financeRequestService.GetDetailsByRequestorAsync(id, userId);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }
    }
}