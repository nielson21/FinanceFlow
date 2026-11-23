using FinanceFlow.Authorization;
using FinanceFlow.Configuration;
using FinanceFlow.Constants;
using FinanceFlow.Data;
using FinanceFlow.Services;
using FinanceFlow.Services.Data;
using FinanceFlow.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

var keyBytes = Convert.FromBase64String(jwtSigningKey);

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
}

builder.Services.AddAuthentication()
    .AddJwtBearer("FinanceFlowJwt", options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    });

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<FinanceFlowSignInManager>();

builder.Services.AddAuthentication()
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IFinanceRequestService, FinanceRequestService>();

builder.Services.AddScoped<IAuthorizationHandler, LargeAmountApprovalHandler>();

builder.Services.AddScoped<IFinanceRequestSummaryReader, DapperFinanceRequestSummaryReader>();

builder.Services.AddScoped<IApprovalHistoryWriter, DapperApprovalHistoryWriter>();

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddTransient<IRequestReferenceGenerator, RequestReferenceGenerator>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("LargeAmountApproval", policy =>
    {
        policy.Requirements.Add(new LargeAmountApprovalRequirement(100000m));
    })
    .AddPolicy("ApiBearer", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
        policy.RequireAuthenticatedUser();
    })
    .AddPolicy("ApiJwt", policy =>
    {
        policy.AddAuthenticationSchemes("FinanceFlowJwt");
        policy.RequireAuthenticatedUser();
    }); ;

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));

var app = builder.Build();

var culture = new CultureInfo("en-PH");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

if (app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    context.Database.EnsureCreated();
}

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in UserRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    if (app.Environment.IsDevelopment())
    {
        var requestor = await userManager.FindByEmailAsync("requestor@test.com");

        if (requestor != null)
        {
            if (!await userManager.IsInRoleAsync(requestor, UserRoles.Requestor))
                await userManager.AddToRoleAsync(requestor, UserRoles.Requestor);

            if (await userManager.IsInRoleAsync(requestor, UserRoles.Admin))
                await userManager.RemoveFromRoleAsync(requestor, UserRoles.Admin);
        }

        var admin = await userManager.FindByEmailAsync("admin@test.com");

        if (admin != null && !await userManager.IsInRoleAsync(admin, UserRoles.Admin))
            await userManager.AddToRoleAsync(admin, UserRoles.Admin);

        var approver = await userManager.FindByEmailAsync("approver@test.com");
        if (approver != null)
        {
            if (!await userManager.IsInRoleAsync(approver, UserRoles.Approver))
                await userManager.AddToRoleAsync(approver, UserRoles.Approver);

            var approverClaims = await userManager.GetClaimsAsync(approver);
            var claim = new System.Security.Claims.Claim("CanApproveLargeAmount", "true");
            if (!approverClaims.Any(x => x.Type == claim.Type && x.Value == claim.Value))
                await userManager.AddClaimAsync(approver, claim);
        }

        var approver2 = await userManager.FindByEmailAsync("approver2@test.com");
        if (approver2 != null && !await userManager.IsInRoleAsync(approver2, UserRoles.Approver))
            await userManager.AddToRoleAsync(approver2, UserRoles.Approver);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();