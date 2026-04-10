using MoneyWeb.Data;
using MoneyWeb.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

