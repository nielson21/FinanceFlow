using FinanceFlow.Api.Models;
using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration
{
    public class FinanceRequestsApiIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        [Fact]
        public async Task GetById_OwnRequest_ShouldReturnOkWithRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-001",
                    RequestStatus.Draft,
                    requestorId,
                    "API Test Request",
                    5000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync($"/api/finance-requests/{requestId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dto = await response.Content.ReadFromJsonAsync<FinanceRequestDto>();

            Assert.NotNull(dto);
            Assert.Equal(requestId, dto.Id);
            Assert.Equal("FR-API-001", dto.ReferenceNo);
            Assert.Equal("API Test Request", dto.Title);
            Assert.Equal(5000m, dto.Amount);
            Assert.Equal(RequestStatus.Draft, dto.Status);
        }

        [Fact]
        public async Task GetById_OtherUsersRequest_ShouldReturnNotFound()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string currentUserId = "test-user";
            const string currentUserEmail = "requestor@test.com";
            const string otherUserId = "other-user";
            const string otherUserEmail = "other@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, currentUserId, currentUserEmail);
                CreateAndAddUser(context, otherUserId, otherUserEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-002",
                    RequestStatus.Draft,
                    otherUserId,
                    "Other User API Request",
                    2500m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync($"/api/finance-requests/{requestId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetById_NonExistingRequest_ShouldReturnNotFound()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string role = "Requestor";

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync("/api/finance-requests/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOnlyCurrentUsersRequestsInNewestOrder()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string otherUserId = "other-user";
            const string otherUserEmail = "other@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);
                CreateAndAddUser(context, otherUserId, otherUserEmail);

                var baseDate = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);

                var request1 = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-LIST-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Older Request",
                    1000m);

                request1.CreatedAt = baseDate.AddMinutes(1);

                var request2 = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-LIST-002",
                    RequestStatus.Approved,
                    requestorId,
                    "Newer Request",
                    2000m);

                request2.CreatedAt = baseDate.AddMinutes(2);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-LIST-OTHER",
                    RequestStatus.Draft,
                    otherUserId,
                    "Other User Request",
                    3000m);

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync("/api/finance-requests");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>();

            Assert.NotNull(result);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(1, result.TotalPages);

            Assert.Equal(2, result.Items.Count);

            Assert.Equal("FR-API-LIST-002", result.Items[0].ReferenceNo);
            Assert.Equal("FR-API-LIST-001", result.Items[1].ReferenceNo);

            Assert.DoesNotContain(
                result.Items,
                x => x.ReferenceNo == "FR-API-LIST-OTHER");
        }

        [Fact]
        public async Task Create_ValidRequest_ShouldReturnCreatedAndPersistRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                CreateAndAddUser(context, requestorId, requestorEmail);
                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            var dto = new CreateFinanceRequestDto
            {
                Title = "API Created Request",
                Amount = 4500m
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/finance-requests", dto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var responseDto = await response.Content.ReadFromJsonAsync<FinanceRequestDto>();

            Assert.NotNull(responseDto);
            Assert.Equal("API Created Request", responseDto.Title);
            Assert.Equal(4500m, responseDto.Amount);
            Assert.Equal(RequestStatus.Draft, responseDto.Status);

            Assert.EndsWith(
                $"/api/finance-requests/{responseDto.Id}",
                response.Headers.Location.ToString());

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var savedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == responseDto.Id);

            Assert.Equal(requestorId, savedRequest.RequestorId);
            Assert.Equal("API Created Request", savedRequest.Title);
            Assert.Equal(4500m, savedRequest.Amount);
        }

        [Fact]
        public async Task Create_InvalidRequest_ShouldReturnBadRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                CreateAndAddUser(context, requestorId, requestorEmail);
                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            var dto = new CreateFinanceRequestDto
            {
                Title = null,
                Amount = 0
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/finance-requests", dto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Null(response.Headers.Location);

            using var assertScope = factory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Equal(0, await assertContext.FinanceRequests.CountAsync());
        }

        [Fact]
        public async Task Update_DraftRequest_ShouldReturnNoContentAndPersistChanges()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-PUT-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Original Title",
                    1000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            var dto = new UpdateFinanceRequestDto
            {
                Title = "Updated Title",
                Amount = 7500m
            };

            // Act
            var response = await client.PutAsJsonAsync(
                $"/api/finance-requests/{requestId}",
                dto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var updatedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            Assert.Equal("Updated Title", updatedRequest.Title);
            Assert.Equal(7500m, updatedRequest.Amount);
        }

        [Fact]
        public async Task Update_NonDraftRequest_ShouldReturnNotFoundAndNotChangeRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-PUT-001",
                    RequestStatus.Approved,
                    requestorId,
                    "Original Title",
                    1000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            var dto = new UpdateFinanceRequestDto
            {
                Title = "Updated Title",
                Amount = 7500m
            };

            // Act
            var response = await client.PutAsJsonAsync(
                $"/api/finance-requests/{requestId}",
                dto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var updatedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            Assert.Equal("Original Title", updatedRequest.Title);
            Assert.Equal(1000m, updatedRequest.Amount);
            Assert.Equal(RequestStatus.Approved, updatedRequest.Status);
        }

        [Fact]
        public async Task Update_OtherUsersRequest_ShouldReturnNotFoundAndNotChangeRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string otherUserId = "other-user";
            const string otherUserEmail = "other@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);
                CreateAndAddUser(context, otherUserId, otherUserEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-PUT-OTHER-001",
                    RequestStatus.Draft,
                    otherUserId,
                    "Other User Original Title",
                    1000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            var dto = new UpdateFinanceRequestDto
            {
                Title = "Updated Title",
                Amount = 7500m
            };

            // Act
            var response = await client.PutAsJsonAsync(
                $"/api/finance-requests/{requestId}",
                dto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var savedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            Assert.Equal("Other User Original Title", savedRequest.Title);
            Assert.Equal(1000m, savedRequest.Amount);
            Assert.Equal(RequestStatus.Draft, savedRequest.Status);
        }

        [Fact]
        public async Task Update_InvalidRequest_ShouldReturnBadRequestAndNotChangeRequest()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            int requestId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var request = CreateAndAddFinanceRequest(
                    context,
                    "FR-API-PUT-INVALID-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Original Title",
                    1000m);

                await context.SaveChangesAsync();

                requestId = request.Id;
            }

            var client = GetHttpClient(factory, role);

            var dto = new UpdateFinanceRequestDto
            {
                Title = null,
                Amount = 0
            };

            // Act
            var response = await client.PutAsJsonAsync(
                $"/api/finance-requests/{requestId}",
                dto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var assertScope = factory.Services.CreateScope();

            var assertContext = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var savedRequest = await assertContext.FinanceRequests
                .SingleAsync(x => x.Id == requestId);

            Assert.Equal("Original Title", savedRequest.Title);
            Assert.Equal(1000m, savedRequest.Amount);
            Assert.Equal(RequestStatus.Draft, savedRequest.Status);
        }

        [Fact]
        public async Task GetAll_WithPagination_ShouldReturnRequestedPage()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var baseDate = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);

                for (var i = 1; i <= 5; i++)
                {
                    var request = CreateAndAddFinanceRequest(
                        context,
                        $"FR-API-PAGE-{i:000}",
                        RequestStatus.Draft,
                        requestorId,
                        $"Request {i}",
                        i * 1000m);

                    request.CreatedAt = baseDate.AddMinutes(i);
                }

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync(
                "/api/finance-requests?page=2&pageSize=2");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>();

            Assert.NotNull(result);

            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.TotalPages);

            Assert.Equal(2, result.Items.Count);

            Assert.Equal("FR-API-PAGE-003", result.Items[0].ReferenceNo);
            Assert.Equal("FR-API-PAGE-002", result.Items[1].ReferenceNo);
        }

        [Fact]
        public async Task GetAll_WithStatusFilter_ShouldReturnOnlyMatchingRequests()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-FILTER-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Draft Request 1",
                    1000m);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-FILTER-002",
                    RequestStatus.Approved,
                    requestorId,
                    "Approved Request",
                    2000m);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-FILTER-003",
                    RequestStatus.Draft,
                    requestorId,
                    "Draft Request 2",
                    3000m);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-FILTER-004",
                    RequestStatus.PendingApproval,
                    requestorId,
                    "Pending Request",
                    4000m);

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync(
                "/api/finance-requests?status=Draft");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>();

            Assert.NotNull(result);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);

            Assert.All(
                result.Items,
                x => Assert.Equal(RequestStatus.Draft, x.Status));

            Assert.Contains(
                result.Items,
                x => x.ReferenceNo == "FR-API-FILTER-001");

            Assert.Contains(
                result.Items,
                x => x.ReferenceNo == "FR-API-FILTER-003");
        }

        [Fact]
        public async Task GetAll_WithSearch_ShouldReturnMatchingRequests()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-SEARCH-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Laptop Purchase",
                    50000m);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-API-SEARCH-002",
                    RequestStatus.Draft,
                    requestorId,
                    "Office Supplies",
                    5000m);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-LAPTOP-003",
                    RequestStatus.Approved,
                    requestorId,
                    "Equipment Request",
                    75000m);

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync(
                "/api/finance-requests?search=laptop");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>();

            Assert.NotNull(result);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);

            Assert.Contains(
                result.Items,
                x => x.ReferenceNo == "FR-API-SEARCH-001");

            Assert.Contains(
                result.Items,
                x => x.ReferenceNo == "FR-LAPTOP-003");

            Assert.DoesNotContain(
                result.Items,
                x => x.ReferenceNo == "FR-API-SEARCH-002");
        }

        [Theory]
        [InlineData("?page=0")]
        [InlineData("?pageSize=101")]
        public async Task GetAll_WithInvalidPagination_ShouldReturnBadRequest(
    string queryString)
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync(
                $"/api/finance-requests{queryString}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_Unauthenticated_ShouldReturnUnauthorized()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            var client = GetHttpClient(factory);

            // Act
            var response = await client.GetAsync("/api/finance-requests");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAll_WithCombinedFiltersAndPagination_ShouldReturnMatchingPage()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            const string requestorId = "test-user";
            const string requestorEmail = "requestor@test.com";
            const string role = "Requestor";

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CreateAndAddUser(context, requestorId, requestorEmail);

                var baseDate = new DateTime(
                    2026,
                    10,
                    26,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc);

                var request1 = CreateAndAddFinanceRequest(
                    context,
                    "FR-COMBINED-001",
                    RequestStatus.Draft,
                    requestorId,
                    "Laptop Request 1",
                    1000m);

                request1.CreatedAt = baseDate.AddMinutes(1);

                var request2 = CreateAndAddFinanceRequest(
                    context,
                    "FR-COMBINED-002",
                    RequestStatus.Draft,
                    requestorId,
                    "Laptop Request 2",
                    2000m);

                request2.CreatedAt = baseDate.AddMinutes(2);

                var request3 = CreateAndAddFinanceRequest(
                    context,
                    "FR-COMBINED-003",
                    RequestStatus.Draft,
                    requestorId,
                    "Laptop Request 3",
                    3000m);

                request3.CreatedAt = baseDate.AddMinutes(3);

                var request4 = CreateAndAddFinanceRequest(
                    context,
                    "FR-COMBINED-004",
                    RequestStatus.Approved,
                    requestorId,
                    "Laptop Request 4",
                    4000m);

                request4.CreatedAt = baseDate.AddMinutes(4);

                CreateAndAddFinanceRequest(
                    context,
                    "FR-COMBINED-005",
                    RequestStatus.Draft,
                    requestorId,
                    "Office Supplies",
                    5000m);

                await context.SaveChangesAsync();
            }

            var client = GetHttpClient(factory, role);

            // Act
            var response = await client.GetAsync(
                "/api/finance-requests?status=Draft&search=laptop&page=2&pageSize=2");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content
                .ReadFromJsonAsync<PagedResponseDto<FinanceRequestDto>>();

            Assert.NotNull(result);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(2, result.TotalPages);

            Assert.Single(result.Items);

            Assert.Equal(
                "FR-COMBINED-001",
                result.Items[0].ReferenceNo);

            Assert.Equal(
                RequestStatus.Draft,
                result.Items[0].Status);
        }
    }
}
