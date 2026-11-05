using FinanceFlow.Data;
using FinanceFlow.Services.Data;
using FinanceFlow.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceFlow.Tests.Integration;

public class FinanceFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<IFinanceRequestSummaryReader>();
            services.RemoveAll<IApprovalHistoryWriter>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.AddScoped<IFinanceRequestSummaryReader, TestFinanceRequestSummaryReader>();
            services.AddScoped<IApprovalHistoryWriter, TestApprovalHistoryWriter>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    TestAuthHandler.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });

            services.PostConfigure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("ApiBearer", policy =>
                {
                    policy.AddAuthenticationSchemes(TestAuthHandler.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });
            });
        });
    }
}