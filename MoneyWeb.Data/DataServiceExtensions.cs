using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using MoneyWeb.Data.Repositories;
using MoneyWeb.Data.Repositories.Interfaces;
using MoneyWeb.Data.Services;

namespace MoneyWeb.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddMoneyWebData(this IServiceCollection services, string connectionString)
    {
        // Teach Dapper how to handle DateOnly
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        // Repositories
        services.AddScoped<IDebtRepository>(_ => new DebtRepository(connectionString));
        services.AddScoped<IBillRepository>(_ => new BillRepository(connectionString));
        services.AddScoped<ILoanRepository>(_ => new LoanRepository(connectionString));
        services.AddScoped<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddScoped<ILoginTokenRepository>(_ => new LoginTokenRepository(connectionString));
        services.AddScoped<IBankAccountRepository>(_ => new BankAccountRepository(connectionString));
        services.AddScoped<IDebtGroupRepository>(_ => new DebtGroupRepository(connectionString));
        services.AddScoped<IIncomeRepository>(_ => new IncomeRepository(connectionString));
        services.AddScoped<IUserGroupRepository>(_ => new UserGroupRepository(connectionString));
        services.AddScoped<IShareRepository>(_ => new ShareRepository(connectionString));
        services.AddScoped<ISharingContactRepository>(_ => new SharingContactRepository(connectionString));
        services.AddScoped<ICashFlowRepository>(_ => new CashFlowRepository(connectionString));
        services.AddScoped<ISinkingFundRepository, SinkingFundRepository>(
            _ => new SinkingFundRepository(connectionString));
        services.AddScoped<IKidRepository>(_ => new KidRepository(connectionString));

        // Services
        services.AddSingleton<DebtPayoffService>();
        services.AddSingleton<BillProjectionService>();
        services.AddScoped<IDebtProjectionService, DebtProjectionService>();

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
