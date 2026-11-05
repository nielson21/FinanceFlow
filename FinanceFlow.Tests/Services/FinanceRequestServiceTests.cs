using FinanceFlow.Data;
using FinanceFlow.Models;
using FinanceFlow.Services;
using FinanceFlow.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FinanceFlow.Tests.Services
{
    public class FinanceRequestServiceTests
    {
        [Fact]
        public async Task SubmitAsync_ShouldChangeDraftToPendingApproval()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var request = new FinanceRequest
            {
                Id = 1,
                ReferenceNo = "FR-TEST-001",
                Title = "Test Request",
                Amount = 1000,
                Status = RequestStatus.Draft,
                RequestorId = "user-1"
            };

            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var fakeTime = new FakeTimeProvider(
                new DateTimeOffset(
                    2030, 1, 1,
                    0, 0, 0,
                    TimeSpan.Zero));

            var referenceGenerator = new FakeRequestReferenceGenerator();
            var summaryReader = new TestFinanceRequestSummaryReader(context);
            var historyWriter = new TestApprovalHistoryWriter(context);

            var service = new FinanceRequestService(
                context,
                fakeTime,
                referenceGenerator,
                NullLogger<FinanceRequestService>.Instance,
                summaryReader,
                historyWriter);

            // Act
            var result = await service.SubmitAsync(
                request.Id,
                "user-1");

            // Assert
            Assert.True(result.IsSuccess);

            Assert.Equal(
                RequestStatus.PendingApproval,
                request.Status);

            var history =
                Assert.Single(context.ApprovalHistories);

            Assert.Equal(RequestStatus.Draft, history.FromStatus);
            Assert.Equal(RequestStatus.PendingApproval, history.ToStatus);
            Assert.Equal("user-1", history.ActionByUserId);
        }

        private class FakeRequestReferenceGenerator
        : IRequestReferenceGenerator
        {
            public string Generate()
            {
                return "FR-TEST-001";
            }
        }

        [Fact]
        public async Task SubmitAsync_ShouldFail_WhenRequestBelongsToAnotherUser()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var request = new FinanceRequest
            {
                Id = 1,
                ReferenceNo = "FR-TEST-002",
                Title = "Another User Request",
                Amount = 1500,
                Status = RequestStatus.Draft,
                RequestorId = "user-1"
            };

            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var fakeTime = new FakeTimeProvider();
            var referenceGenerator = new FakeRequestReferenceGenerator();
            var summaryReader = new TestFinanceRequestSummaryReader(context);
            var historyWriter = new TestApprovalHistoryWriter(context);

            var service = new FinanceRequestService(
                context,
                fakeTime,
                referenceGenerator,
                NullLogger<FinanceRequestService>.Instance,
                summaryReader,
                historyWriter);

            // Act
            var result = await service.SubmitAsync(
                request.Id,
                "user-2");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(RequestStatus.Draft, request.Status);
            Assert.Empty(context.ApprovalHistories);
        }

        [Theory]
        [InlineData(RequestStatus.PendingApproval)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.Rejected)]
        public async Task SubmitAsync_ShouldFail_WhenRequestIsNotDraft(RequestStatus currentStatus)
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var request = new FinanceRequest
            {
                Id = 1,
                ReferenceNo = "FR-TEST-003",
                Title = "Non-Draft Request",
                Amount = 2000,
                Status = currentStatus,
                RequestorId = "user-1"
            };

            context.FinanceRequests.Add(request);
            await context.SaveChangesAsync();

            var fakeTime = new FakeTimeProvider();
            var referenceGenerator = new FakeRequestReferenceGenerator();
            var summaryReader = new TestFinanceRequestSummaryReader(context);
            var historyWriter = new TestApprovalHistoryWriter(context);

            var service = new FinanceRequestService(
                context,
                fakeTime,
                referenceGenerator,
                NullLogger<FinanceRequestService>.Instance,
                summaryReader,
                historyWriter);

            // Act
            var result = await service.SubmitAsync(
                request.Id,
                "user-1");

            // Assert
            Assert.False(result.IsSuccess);

            Assert.Equal(
                currentStatus,
                request.Status);

            Assert.Empty(context.ApprovalHistories);
        }
    }
}
