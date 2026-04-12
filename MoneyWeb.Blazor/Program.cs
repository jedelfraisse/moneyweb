using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using MoneyWeb.Data;
using MoneyWeb.Blazor.Components;
using MoneyWeb.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Entra ID authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Forward the 'prompt' parameter from AuthenticationProperties to the OIDC protocol message.
// This lets SignIn pass prompt=select_account so the account picker always appears.
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var prev = options.Events.OnRedirectToIdentityProvider;
    options.Events.OnRedirectToIdentityProvider = async ctx =>
    {
        if (prev is not null) await prev(ctx);
        if (ctx.Properties.Parameters.TryGetValue("prompt", out var prompt))
            ctx.ProtocolMessage.Prompt = prompt?.ToString();
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<LoanInterestService>();
builder.Services.AddHostedService<InterestAccrualBackgroundService>();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


