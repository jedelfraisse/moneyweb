using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MoneyWeb.Data.Repositories;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddMoneyWebData(this IServiceCollection services, string connectionString)
    {
        // Repositories
        services.AddScoped<IDebtRepository>(_ => new DebtRepository(connectionString));
        services.AddScoped<IBillRepository>(_ => new BillRepository(connectionString));
        services.AddScoped<ILoanRepository>(_ => new LoanRepository(connectionString));

        // FluentMigrator
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DataServiceExtensions).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    /// <summary>Applies any pending migrations. Call once at startup.</summary>
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}
