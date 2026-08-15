using Microsoft.AspNetCore.Authentication.Cookies;
using MoneyWeb.Data;
using MoneyWeb.Blazor.Auth;
using MoneyWeb.Blazor.Components;
using MoneyWeb.Blazor.Services;
using MoneyWeb.Blazor.Services.Email;
using Sqids;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Self-hosted passwordless authentication (magic link + one-time code) with a
// persistent 30-day sliding-expiration cookie — replaces Entra External ID (CIAM).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "MoneyWeb.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Dev's default launch profile is plain HTTP (localhost:5233) — a Secure-always
        // cookie wouldn't be set there. Relax only in Development.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<PasswordlessAuthService>();
builder.Services.AddScoped<LoanInterestService>();
builder.Services.AddHostedService<InterestAccrualBackgroundService>();

// Email — SMTP when configured (e.g. a local dev catcher like smtp4dev, or a real relay in
// production); otherwise falls back to logging the code/link to the console + /dev/sent-mail.
// See MoneyWeb.Blazor/Services/Email/.
builder.Services.AddSingleton<DevMailbox>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, DevEmailSender>();
}

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
    if (string.IsNullOrWhiteSpace(app.Configuration["Smtp:Host"]))
        app.Logger.LogWarning("No Smtp:Host configured outside Development — login emails will only be logged, not sent. Configure Smtp:Host/Port/From before serving real users.");
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
