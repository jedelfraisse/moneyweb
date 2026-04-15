using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using MoneyWeb.Data;
using MoneyWeb.Blazor.Components;
using MoneyWeb.Blazor.Services;
using Sqids;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Entra External ID (CIAM) authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events.OnSignedOutCallbackRedirect = ctx =>
    {
        ctx.Response.Redirect("/");
        ctx.HandleResponse();
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<LoanInterestService>();
builder.Services.AddHostedService<InterestAccrualBackgroundService>();

// Sqids — obfuscates sequential integer IDs in URLs
builder.Services.AddSingleton(new SqidsEncoder<int>(new SqidsOptions
{
    Alphabet = builder.Configuration["Sqids:Alphabet"]
        ?? "kLpQ7mR3nXv8wfYaZbTuGcHjEd2Oi9NgCeM6hAoB5sItJ1KDy4PUz0VlFrxWSq",
    MinLength = int.TryParse(builder.Configuration["Sqids:MinLength"], out var sqidsMinLen)
        ? sqidsMinLen : 5
}));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("moneyweb")
    ?? throw new InvalidOperationException("Connection string 'moneyweb' not found.");

builder.Services.AddMoneyWebData(connectionString);

var app = builder.Build();

app.MapDefaultEndpoints();

// Apply any pending FluentMigrator migrations at startup
app.Services.ApplyMigrations();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();

// Publisher domain verification for Microsoft identity
app.MapGet("/.well-known/microsoft-identity-association.json", () =>
    Results.Json(new
    {
        associatedApplications = new[] { new { applicationId = "2c0a4efd-f605-48c4-bd31-62d05a6bbcc1" } }
    }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


