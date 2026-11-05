using FinanceFlow.Api.Models;
using FinanceFlow.Data;
using FinanceFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Api.Controllers
{
    [ApiController]
    [Route("api/finance-requests")]
    [Authorize(Policy = "ApiBearer")]
    public class FinanceRequestsApiController(IFinanceRequestService financeRequestService,
        UserManager<ApplicationUser> userManager) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FinanceRequestDto>> GetById(int id)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var request = await financeRequestService.GetByIdForRequestorAsync(id, userId);

            if (request == null)
            {
                return NotFound();
            }

            var dto = new FinanceRequestDto
            {
                Id = request.Id,
                ReferenceNo = request.ReferenceNo,
                Title = request.Title,
                Amount = request.Amount,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                RowVersion = request.RowVersion
            };

            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<FinanceRequestDto>>> GetAll(
    [FromQuery] FinanceRequestQueryDto query)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await financeRequestService.GetPagedByRequestorAsync(
                userId,
                query.Status,
                query.Search,
                query.Page,
                query.PageSize);

            var response = new PagedResponseDto<FinanceRequestDto>
            {
                Items = [.. result.Items
                    .Select(x => new FinanceRequestDto
                    {
                        Id = x.Id,
                        ReferenceNo = x.ReferenceNo,
                        Title = x.Title,
                        Amount = x.Amount,
                        Status = x.Status,
                        CreatedAt = x.CreatedAt,
                        RowVersion = x.RowVersion
                    })],
                Page = result.CurrentPage,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<FinanceRequestDto>> Create(CreateFinanceRequestDto dto)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var request = await financeRequestService.CreateAsync(dto.Title!, dto.Amount, userId);

            var responseDto = new FinanceRequestDto
            {
                Id = request.Id,
                ReferenceNo = request.ReferenceNo,
                Title = request.Title,
                Amount = request.Amount,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                RowVersion = request.RowVersion
            };

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, responseDto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, UpdateFinanceRequestDto dto)
        {
            var userId = userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var result = await financeRequestService.UpdateDraftAsync(id, userId, dto.Title!, dto.Amount, dto.RowVersion);

                if (!result.IsSuccess)
                {
                    return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    Message = "This request has been modified by another user."
                });
            }

            return NoContent();
        }
    }
}
