using Dapper;
using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services.Data;
using FinanceFlow.Services.Models;
using FinanceFlow.Services.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FinanceFlow.Services
{
    public class FinanceRequestService(ApplicationDbContext context, TimeProvider timeProvider,
        IRequestReferenceGenerator referenceGenerator, ILogger<FinanceRequestService> logger,
        IFinanceRequestSummaryReader summaryReader, IApprovalHistoryWriter historyWriter) : IFinanceRequestService
    {
        public async Task<FinanceRequest> CreateAsync(string title, decimal amount, string userId)
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var request = new FinanceRequest
                {
                    ReferenceNo = referenceGenerator.Generate(),
                    Title = title,
                    Amount = amount,
                    Status = RequestStatus.Draft,
                    CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                    RequestorId = userId
                };

                context.FinanceRequests.Add(request);
                try
                {
                    await context.SaveChangesAsync();
                    return request;
                }
                catch (DbUpdateException ex) when (IsDuplicateReferenceException(ex))
                {
                    context.Entry(request).State = EntityState.Detached;

                    if (attempt == maxAttempts)
                    {
                        throw;
                    }
                }
            }

            return new FinanceRequest();
        }

        private static bool IsDuplicateReferenceException(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return true;
            }

            if (ex.InnerException is SqliteException sqliteEx
                && (sqliteEx.SqliteExtendedErrorCode == 2067))
            {
                return true;
            }

            return false;
        }

        public async Task<ServiceResult> SubmitAsync(int id, string userId)
        {
            var request = await context.FinanceRequests
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.RequestorId == userId &&
                    x.Status == RequestStatus.Draft);

            if (request == null)
            {
                return new ServiceResult(false, null);
            }

            var fromStatus = request.Status;

            request.Status = RequestStatus.PendingApproval;

            context.ApprovalHistories.Add(new ApprovalHistory
            {
                FinanceRequestId = request.Id,
                FromStatus = fromStatus,
                ToStatus = request.Status,
                ActionByUserId = userId,
                ActionAt = timeProvider.GetUtcNow().UtcDateTime
            });

            await context.SaveChangesAsync();

            return new ServiceResult(true, request.ReferenceNo);
        }

        public async Task<ServiceResult> ApproveAsync(int id, string userId, string? comment)
        {
            var request = await context.FinanceRequests
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (request == null)
            {
                return new ServiceResult(false, null);
            }

            if (request.Status != RequestStatus.PendingApproval)
            {
                logger.LogWarning("Approval attempt rejected for finance request {FinanceRequestId} by user {UserId}. Current status: {Status}",
                    request.Id, userId, request.Status);

                return new ServiceResult(false, null);
            }

            var fromStatus = request.Status;
            request.Status = RequestStatus.Approved;

            var approvalHistory = new ApprovalHistory
            {
                FinanceRequestId = request.Id,
                FromStatus = fromStatus,
                ToStatus = request.Status,
                ActionByUserId = userId,
                Comment = comment,
                ActionAt = timeProvider.GetUtcNow().UtcDateTime
            };

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.SaveChangesAsync();

                await historyWriter.InsertAsync(approvalHistory);

                await transaction.CommitAsync();

                logger.LogInformation("Finance request {FinanceRequestId} approved by user {UserId}", request.Id, userId);

                return new ServiceResult(true, request.ReferenceNo);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult> RejectAsync(int id, string userId, string? comment)
        {
            var request = await context.FinanceRequests
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
            {
                return new ServiceResult(false, null);
            }

            if (request.Status != RequestStatus.PendingApproval)
            {
                logger.LogWarning(
                    "Rejection attempt rejected for finance request {FinanceRequestId} by user {UserId}. Current status: {Status}",
                    request.Id,
                    userId,
                    request.Status);

                return new ServiceResult(false, null);
            }

            var fromStatus = request.Status;

            request.Status = RequestStatus.Rejected;

            context.ApprovalHistories.Add(new ApprovalHistory
            {
                FinanceRequestId = request.Id,
                FromStatus = fromStatus,
                ToStatus = request.Status,
                ActionByUserId = userId,
                Comment = comment,
                ActionAt = timeProvider.GetUtcNow().UtcDateTime
            });

            await context.SaveChangesAsync();

            return new ServiceResult(true, request.ReferenceNo);
        }

        public async Task<FinanceRequest?> GetByIdAsync(int id)
        {
            var financeRequest = await context.FinanceRequests.SingleOrDefaultAsync(x => x.Id == id);
            return financeRequest;
        }

        public async Task<FinanceRequest?> GetByIdForRequestorAsync(int id, string requestorId)
        {
            var financeRequest = await context.FinanceRequests.SingleOrDefaultAsync(x => x.Id == id && x.RequestorId == requestorId);
            return financeRequest;
        }

        public async Task<FinanceRequest?> GetEditableByRequestorAsync(int id, string requestorId)
        {
            var financeRequest = await context.FinanceRequests
                .SingleOrDefaultAsync(x => x.Id == id
                    && x.RequestorId == requestorId
                    && x.Status == RequestStatus.Draft);

            return financeRequest;
        }

        public async Task<ServiceResult> UpdateDraftAsync(int id, string requestorId, string title, decimal amount, byte[] rowVersion)
        {
            var request = await GetEditableByRequestorAsync(id, requestorId);

            if (request == null)
            {
                return new ServiceResult(false, null);
            }

            request.Title = title;
            request.Amount = amount;

            context.Entry(request)
                .Property(x => x.RowVersion)
                .OriginalValue = rowVersion;

            await context.SaveChangesAsync();
            return new ServiceResult(true, request.ReferenceNo);
        }

        public async Task<FinanceRequest?> GetDetailsByRequestorAsync(int id, string requestorId)
        {
            var financeRequest = await context.FinanceRequests
                    .Include(x => x.ApprovalHistories)
                        .ThenInclude(x => x.ActionByUser)
                    .SingleOrDefaultAsync(x => x.Id == id && x.RequestorId == requestorId);

            return financeRequest;
        }

        public async Task<FinanceRequestDashboardSummary> GetDashboardSummaryAsync(string userId)
        {
            var summary = await summaryReader.GetByRequestorAsync(userId);

            var recentRequests = await context.FinanceRequests
                .Where(x => x.RequestorId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new RecentFinanceRequestSummary
                {
                    Id = x.Id,
                    ReferenceNo = x.ReferenceNo,
                    Title = x.Title,
                    Amount = x.Amount,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            summary ??= new FinanceRequestDashboardSummary();
            summary.RecentRequests = recentRequests;

            return summary;
        }

        public async Task<PaginatedResult<FinanceRequestListItem>> GetPagedByRequestorAsync(string userId, RequestStatus? status, string? search, int page, int pageSize)
        {
            page = Math.Max(page, 1);

            var query = context.FinanceRequests
                .Where(x => x.RequestorId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{search.Trim()}%";

                query = query.Where(x => EF.Functions.Like(x.ReferenceNo, searchPattern)
                    || EF.Functions.Like(x.Title, searchPattern));
            }

            var totalCount = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FinanceRequestListItem
                {
                    Id = x.Id,
                    ReferenceNo = x.ReferenceNo,
                    Title = x.Title,
                    Amount = x.Amount,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    RowVersion = x.RowVersion
                })
                .ToListAsync();

            return new PaginatedResult<FinanceRequestListItem>
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
        }

        public async Task<List<FinanceRequestListItem>> GetByRequestorAsync(string requestorId)
        {
            return await context.FinanceRequests
                .Where(x => x.RequestorId == requestorId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new FinanceRequestListItem
                {
                    Id = x.Id,
                    ReferenceNo = x.ReferenceNo,
                    Title = x.Title,
                    Amount = x.Amount,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }
    }
}