using FinanceFlow.Client.Clients;
using FinanceFlow.Client.Exceptions;
using FinanceFlow.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var baseUrl = builder.Configuration["FinanceFlowApi:BaseUrl"]
    ?? throw new InvalidOperationException("FinanceFlow API BaseUrl is missing.");

builder.Services.AddHttpClient<FinanceFlowApiClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
});

using var host = builder.Build();

var apiClient = host.Services.GetRequiredService<FinanceFlowApiClient>();

Console.Write("Email: ");
var email = Console.ReadLine() ?? string.Empty;

Console.Write("Password: ");
var password = ReadPassword();

FillCredentials(ref email, ref password, builder);

try
{
    await apiClient.LoginAsync(email, password);

    Console.WriteLine("Login successful!");

    await GetRequests(apiClient);
}
catch (ApiValidationException ex)
{
    Console.WriteLine("Validation failed:");
    foreach (var error in ex.Errors)
    {
        foreach (var message in error.Value)
        {
            Console.WriteLine($"- {error.Key}: {message}");
        }
    }
}
catch (HttpRequestException ex)
{
    var message = ex.StatusCode switch
    {
        HttpStatusCode.BadRequest => "Invalid request data.",
        HttpStatusCode.Unauthorized => "Authentication failed or token expired.",
        HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
        HttpStatusCode.NotFound => "Finance request not found or cannot be edited.",
        HttpStatusCode.Conflict => ex.Message,
        null => "Unable to connect to FinanceFlow API.",
        _ => $"API request failed. Status: {ex.StatusCode}"
    };

    Console.WriteLine(message);
}

Console.ReadKey(true);

static string ReadPassword()
{
    var password = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }

    Console.WriteLine();

    return password.ToString();
}

static void FillCredentials(ref string email, ref string password, HostApplicationBuilder builder)
{
    if (String.IsNullOrWhiteSpace(email)) email = builder.Configuration["FinanceFlowCredentials:Email"]
            ?? throw new InvalidOperationException("Email is missing.");
    if (String.IsNullOrWhiteSpace(password)) password = builder.Configuration["FinanceFlowCredentials:Password"]
            ?? throw new InvalidOperationException("Password is missing.");
}

static async Task GetRequests(FinanceFlowApiClient apiClient)
{
    var result = await apiClient.GetRequestsAsync();

    Console.WriteLine($"Total requests: {result.TotalCount}");

    foreach (var request in result.Items)
    {
        Console.WriteLine($"{request.ReferenceNo} | {request.Title} | {request.Amount:N2} | {request.Status}");
    }
    Console.ReadKey(true);
}