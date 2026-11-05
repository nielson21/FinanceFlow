using FinanceFlow.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration
{
    public class AdminIntegrationTests (FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        [Fact]
        public async Task Deactivate_AnotherUser_ShouldDeactivate()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminId = "test-user";
            var adminEmail = "admin@test.com";
            var anotherUserId = "another-user";
            var anotherUserEmail = "another@test.com";

            var adminUser = CreateUser(adminId, adminEmail);
            context.Users.Add(adminUser);
            var anotherUser = CreateUser(anotherUserId, anotherUserEmail);
            context.Users.Add(anotherUser);
            await context.SaveChangesAsync();

            var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var token = await GetHtmlValueAsync(client, "/Admin/Users");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["id"] = anotherUserId
                });

            // Act
            var response = await client.PostAsync("/Admin/Users/Deactivate", content);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            context.ChangeTracker.Clear();

            var deactivatedUser = context.Users.Single(x => x.Id == anotherUserId);
            var reloadedAdminUser = context.Users.Single(x => x.Id == adminId);

            // Assert
            Assert.False(deactivatedUser.IsActive);
            Assert.True(reloadedAdminUser.IsActive);
        }

        [Fact]
        public async Task Reactivate_AnotherUser_ShouldReactivate()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminId = "test-user";
            var adminEmail = "admin@test.com";
            var anotherUserId = "another-user";
            var anotherUserEmail = "another@test.com";

            var adminUser = CreateUser(adminId, adminEmail);
            context.Users.Add(adminUser);
            var anotherUser = CreateUser(anotherUserId, anotherUserEmail);
            anotherUser.IsActive = false;
            context.Users.Add(anotherUser);
            await context.SaveChangesAsync();

            var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var token = await GetHtmlValueAsync(client, "/Admin/Users");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["id"] = anotherUserId
                });

            // Act
            var response = await client.PostAsync("/Admin/Users/Reactivate", content);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            context.ChangeTracker.Clear();

            var reactivatedUser = context.Users.Single(x => x.Id == anotherUserId);
            var reloadedAdminUser = context.Users.Single(x => x.Id == adminId);

            // Assert
            Assert.True(reactivatedUser.IsActive);
            Assert.True(reloadedAdminUser.IsActive);
        }

        [Fact]
        public async Task Deactivate_Self_ShouldNotDeactivate()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminId = "test-user";
            var adminEmail = "admin@test.com";

            var adminUser = CreateAndAddUser(context, adminId, adminEmail);
            await context.SaveChangesAsync();

            var client = GetHttpClient(factory, "Admin");
            var content = await GenerateContentAsync(client, "/Admin/Users",
                new Dictionary<string, string>()
                {
                    ["id"] = adminId
                });

            // Act
            var response = await client.PostAsync("/Admin/Users/Deactivate", content);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var indexResponse = await client.GetAsync("/Admin/Users");
            indexResponse.EnsureSuccessStatusCode();

            var html = await indexResponse.Content.ReadAsStringAsync();

            Assert.Contains("You cannot deactivate your own account.", html);

            context.ChangeTracker.Clear();

            var reloadedAdminUser = context.Users.Single(x => x.Id == adminId);

            // Assert
            Assert.True(reloadedAdminUser.IsActive);
        }

        [Fact]
        public async Task Edit_SameUserWithStaleConcurrencyStamp_ShouldShowConcurrencyError()
        {
            // Arrange
            await ResetDatabaseAsync(factory.Services);

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userId = "requestor-user";
            var userEmail = "requestor@test.com";
            var newUsernameA = "requestor001";
            var newUsernameB = "requestor002";

            var user = CreateUser(userId, userEmail);
            var createResult = await userManager.CreateAsync(user);
            Assert.True(createResult.Succeeded);

            // Client get ConcurrencyStamp /Admins/Users/Edit/001
            var client = GetHttpClient(factory, "Admin");
            var concurrencyStamp = await GetHtmlValueAsync(client, $"/Admin/Users/Edit/{userId}", HtmlName.ConcurrencyStamp);

            var content = await GenerateContentAsync(client, $"/Admin/Users/Edit/{userId}",
                new Dictionary<string, string>()
                {
                    ["id"] = userId,
                    ["ConcurrencyStamp"] = concurrencyStamp,
                    ["Email"] = userEmail,
                    ["UserName"] = newUsernameB
                });

            // Act
            // UserManager update user
            user.UserName = newUsernameA;
            var updateResult = await userManager.UpdateAsync(user);
            Assert.True(updateResult.Succeeded);
            Assert.NotEqual(concurrencyStamp, user.ConcurrencyStamp);

            // Client post edit with stale ConcurrencyStamp
            var postResponse = await client.PostAsync($"/Admin/Users/Edit/{userId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("This user was modified by another administrator.", html);

            using var assertScope = factory.Services.CreateScope();
            var reloaded = await ReloadUserAsync(assertScope, userId);

            Assert.Equal(newUsernameA, reloaded.user.UserName);
        }
    }
}
