using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static FinanceFlow.Tests.Helpers.IntegrationTestHelpers;

namespace FinanceFlow.Tests.Integration
{
    public partial class IdentityIntegrationTests(FinanceFlowWebApplicationFactory factory) : IClassFixture<FinanceFlowWebApplicationFactory>
    {
        private readonly FinanceFlowWebApplicationFactory _factory = factory;

        [Fact]
        public async Task Login_InactiveUser_ShouldFail()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userId = "inactive-user";
            var userEmail = "inactive@test.com";
            var password = "P@ssw0rd";

            var user = new ApplicationUser()
            {
                Id = userId,
                Email = userEmail,
                UserName = userEmail,
                EmailConfirmed = true,
                IsActive = false
            };

            var createResult = await usermanager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded);

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            var token = await GetHtmlValueAsync(client, "/Identity/Account/Login");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["Input.Email"] = userEmail,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false"
                });

            // Act
            var response = await client.PostAsync("/Identity/Account/Login", content);

            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Invalid login attempt.", html);
        }

        [Fact]
        public async Task Deactivate_User_ShouldChangeSecurityStamp()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userId = "another-user";
            var userEmail = "another@test.com";

            var user = CreateUser(userId, userEmail);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var securityStamp = user.SecurityStamp;

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            var token = await GetHtmlValueAsync(client, "/Admin/Users");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["id"] = userId
                });

            // Act
            var response = await client.PostAsync("/Admin/Users/Deactivate", content);

            context.ChangeTracker.Clear();

            var updatedUser = await context.Users.SingleAsync(x => x.Id == userId);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotEqual(securityStamp, updatedUser.SecurityStamp);
            Assert.False(updatedUser.IsActive);
        }

        [Fact]
        public async Task Deactivate_LoggedInUser_ShouldInvalidateExistingSession()
        {
            // Arrange
            using var realFactory = new FinanceFlowRealAuthWebApplicationFactory();

            await ResetDatabaseAsync(realFactory.Services);

            using var scope = realFactory.Services.CreateScope();
            var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] userIds = ["another-user", "admin-user"];
            string[] userEmails = ["another@test.com", "admin@test.com"];
            string[] roles = ["Requestor", "Admin"];
            var password = "P@ssw0rd";

            for (int i = 0; i < 2; i++)
            {
                var user = new ApplicationUser()
                {
                    Id = userIds[i],
                    Email = userEmails[i],
                    UserName = userEmails[i],
                    EmailConfirmed = true,
                    IsActive = true
                };

                var createResult = await usermanager.CreateAsync(user, password);
                Assert.True(createResult.Succeeded);

                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roles[i]));
                Assert.True(createRoleResult.Succeeded);

                var addRoleResult = await usermanager.AddToRoleAsync(user, roles[i]);
                Assert.True(addRoleResult.Succeeded);
            }

            HttpClient[] clients = new HttpClient[2];

            for (int i = 0; i < clients.Length; i++)
            {
                var client = clients[i];

                client = realFactory.CreateClient(new()
                {
                    AllowAutoRedirect = false
                });

                var token = await GetHtmlValueAsync(client, "/Identity/Account/Login");

                var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["Input.Email"] = userEmails[i],
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false"
                });

                var userLoginResponse = await client.PostAsync("/Identity/Account/Login", content);
                Assert.Equal(HttpStatusCode.Redirect, userLoginResponse.StatusCode);

                clients[i] = client;
            }

            var userClient = clients[0];
            var adminClient = clients[1];

            var userRequestResponse = await userClient.GetAsync("/FinanceRequests");
            userRequestResponse.EnsureSuccessStatusCode();

            var adminToken = await GetHtmlValueAsync(adminClient, "/Admin/Users");
            var adminContent = GenerateContent(adminToken,
                new Dictionary<string, string>()
                {
                    ["id"] = userIds[0]
                });

            // Act
            var adminResponse = await adminClient.PostAsync("/Admin/Users/Deactivate", adminContent);
            Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);

            var reloadedUserRequestResponse = await userClient.GetAsync("/FinanceRequests");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, reloadedUserRequestResponse.StatusCode);
            Assert.NotNull(reloadedUserRequestResponse.Headers.Location);
            Assert.Contains("/Identity/Account/Login", reloadedUserRequestResponse.Headers.Location.ToString());
        }

        [Fact]
        public async Task Login_WrongPassword_ThreeTimes_ShouldLockoutUser()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userId = "test-user";
            var userEmail = "user@test.com";
            var password = "P@ssw0rd";
            var wrongPassword = "P@ssw0rd1";
            var maxAttempts = 3;

            var user = new ApplicationUser()
            {
                Id = userId,
                Email = userEmail,
                UserName = userEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await usermanager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded);

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            var token = await GetHtmlValueAsync(client, "/Identity/Account/Login");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["Input.Email"] = userEmail,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false"
                });

            var wrongPasswordContent = GenerateContent(token,
               new Dictionary<string, string>()
               {
                   ["Input.Email"] = userEmail,
                   ["Input.Password"] = wrongPassword,
                   ["Input.RememberMe"] = "false"
               });

            HttpResponseMessage wrongPasswordResponse = new();

            // Act
            for (int i = 0; i < maxAttempts; i++)
            {
                wrongPasswordResponse = await client.PostAsync("/Identity/Account/Login", wrongPasswordContent);

                if (i == maxAttempts - 1)
                {
                    Assert.Equal(HttpStatusCode.Redirect, wrongPasswordResponse.StatusCode);
                    Assert.NotNull(wrongPasswordResponse.Headers.Location);
                    Assert.Contains("/Identity/Account/Lockout", wrongPasswordResponse.Headers.Location.ToString());
                }
                else Assert.Equal(HttpStatusCode.OK, wrongPasswordResponse.StatusCode);
            }

            var latestResponse = await client.PostAsync("/Identity/Account/Login", content);

            using var assertScope = _factory.Services.CreateScope();
            var assertUserManager = assertScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var reloadedUser = await assertUserManager.FindByIdAsync(userId);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, latestResponse.StatusCode);
            Assert.NotNull(latestResponse.Headers.Location);
            Assert.Contains("/Identity/Account/Lockout", latestResponse.Headers.Location.ToString());
            Assert.NotNull(reloadedUser);
            Assert.True(await assertUserManager.IsLockedOutAsync(reloadedUser));
        }

        [Fact]
        public async Task Login_AfterFailedAttempts_ShouldResetAccessFailedCount()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var usermanager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            const string userId = "test-user";
            const string userEmail = "user@test.com";
            const string password = "P@ssw0rd";
            const string wrongPassword = "P@ssw0rd1";
            const int failedAttemptsBeforeSuccess = 2;

            var user = new ApplicationUser()
            {
                Id = userId,
                Email = userEmail,
                UserName = userEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await usermanager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded);

            var client = _factory.CreateClient(new()
            {
                AllowAutoRedirect = false
            });

            // Act
            for (int i = 0; i < failedAttemptsBeforeSuccess; i++)
            {
                var wrongPasswordResponse = await HttpLogin(client, userEmail, wrongPassword);
                Assert.Equal(HttpStatusCode.OK, wrongPasswordResponse.StatusCode);
            }

            using (var assertScope = _factory.Services.CreateScope())
            {
                var reloaded = await ReloadUserAsync(assertScope, userId);
                usermanager = reloaded.userManager;
                user = reloaded.user;

                var failedCount = await usermanager.GetAccessFailedCountAsync(user);
                Assert.Equal(failedAttemptsBeforeSuccess, failedCount);
            }

            var latestResponse = await HttpLogin(client, userEmail, password);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, latestResponse.StatusCode);

            using (var assertScope2 = _factory.Services.CreateScope())
            {
                var reloaded = await ReloadUserAsync(assertScope2, userId);
                usermanager = reloaded.userManager;
                user = reloaded.user;

                Assert.Equal(0, await usermanager.GetAccessFailedCountAsync(user));
                Assert.False(await usermanager.IsLockedOutAsync(user));
            }

            Assert.NotNull(latestResponse.Headers.Location);
            Assert.DoesNotContain("/Identity/Account/Lockout", latestResponse.Headers.Location.ToString());
        }

        [Fact]
        public async Task Update_SameUserFromTwoScopes_ShouldDetectConcurrencyConflict()
        {
            // Arrange
            await ResetDatabaseAsync(_factory.Services);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            const string userId = "test-user";
            const string userEmail = "user@test.com";
            const string updatedUserNameA = "newUserName";
            const string updatedUserNameB = "anotherUserName";

            var user = CreateUser(userId, userEmail);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            using var scopeA = _factory.Services.CreateScope();
            var userManagerA = scopeA.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            using var scopeB = _factory.Services.CreateScope();
            var userManagerB = scopeB.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var userFromA = await userManagerA.FindByIdAsync(userId);
            Assert.NotNull(userFromA);
            var userFromB = await userManagerB.FindByIdAsync(userId);
            Assert.NotNull(userFromB);

            // Act & Assert
            userFromA.UserName = updatedUserNameA;
            var updateResultA = await userManagerA.UpdateAsync(userFromA);
            Assert.True(updateResultA.Succeeded);

            userFromB.UserName = updatedUserNameB;
            var updateResultB = await userManagerB.UpdateAsync(userFromB);
            Assert.False(updateResultB.Succeeded);
            Assert.Contains(updateResultB.Errors, x => x.Code == "ConcurrencyFailure");

            using var reloadedScope = _factory.Services.CreateScope();
            var reloaded = await ReloadUserAsync(reloadedScope, userId);

            Assert.Equal(updatedUserNameA, reloaded.user.UserName);
        }
    }
}
