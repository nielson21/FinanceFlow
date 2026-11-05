using FinanceFlow.Data;
using FinanceFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace FinanceFlow.Tests.Helpers
{
    public static partial class IntegrationTestHelpers
    {
        public enum HtmlName
        {
            __RequestVerificationToken,
            ConcurrencyStamp
        }

        public static async Task<string> GetHtmlValueAsync(HttpClient client, string requestUri, HtmlName htmlName = HtmlName.__RequestVerificationToken)
        {
            var response = await client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var pattern = $@"name=""{Regex.Escape(htmlName.ToString())}""[^>]*value=""([^""]+)""";
            var match = Regex.Match(html, pattern);
            Assert.True(match.Success);

            var token = match.Groups[1].Value;
            Assert.False(string.IsNullOrWhiteSpace(token));

            return token;
        }

        public static FormUrlEncodedContent GenerateContent(string token, Dictionary<string, string>? data = null)
        {
            data ??= [];

            data.Add("__RequestVerificationToken", token);

            return GenerateContent(data);
        }

        public static async Task<FormUrlEncodedContent> GenerateContentAsync(HttpClient client, string requestUri, Dictionary<string, string>? data = null)
        {
            var token = await GetHtmlValueAsync(client, requestUri);

            return GenerateContent(token, data);
        }

        public static FormUrlEncodedContent GenerateContent(Dictionary<string, string> data)
        {
            return new FormUrlEncodedContent(data);
        }

        public static async Task<HttpResponseMessage> HttpLogin(HttpClient client, string email, string password, bool rememberMe = false)
        {
            var token = await GetHtmlValueAsync(client, "/Identity/Account/Login");
            var content = GenerateContent(token,
                new Dictionary<string, string>()
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = rememberMe.ToString()
                });

            var response = await client.PostAsync("/Identity/Account/Login", content);
            return response;
        }

        public static async Task<(UserManager<ApplicationUser> userManager, ApplicationUser user)> ReloadUserAsync(IServiceScope scope, string userId)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.NotNull(userManager);

            var user = await LoadUser(userManager, userId);

            return (userManager, user);
        }

        public static async Task<ApplicationUser> LoadUser(UserManager<ApplicationUser> userManager, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);

            return user;
        }

        public static ApplicationUser CreateUser(string id, string email)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email
            };
        }

        public static ApplicationUser CreateAndAddUser(ApplicationDbContext context, string id, string email)
        {
            var user = CreateUser(id, email);

            context.Users.Add(user);

            return user;
        }

        public static FinanceRequest CreateFinanceRequest(string referenceNo, RequestStatus requestStatus, string requestorId, string title = "Test Finance Request",
            decimal amount = 1000m)
        {
            return new FinanceRequest
            {
                ReferenceNo = referenceNo,
                Title = title,
                Amount = amount,
                Status = requestStatus,
                RequestorId = requestorId
            };
        }

        public static FinanceRequest CreateAndAddFinanceRequest(ApplicationDbContext context, string referenceNo, RequestStatus requestStatus, string requestorId, string title = "Test Finance Request",
            decimal amount = 1000m)
        {
            var request = CreateFinanceRequest(referenceNo, requestStatus, requestorId, title, amount);

            context.FinanceRequests.Add(request);

            return request;
        }

        public static async Task ResetDatabaseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public static HttpClient GetHttpClient<T>(T factory, string? role = null, bool canApproveLargeAmount = false, bool allowAutoRedirect = false) where T : WebApplicationFactory<Program>
        {
            var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = allowAutoRedirect
            });

            if (!string.IsNullOrWhiteSpace(role))
            {
                client.DefaultRequestHeaders.Add("X-Test-Role", role);
            }

            if (canApproveLargeAmount)
            {
                client.DefaultRequestHeaders.Add("X-Test-CanApproveLargeAmount", "true");
            }

            return client;
        }
    }
}
