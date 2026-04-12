namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Runs once at startup and then every day at 2 AM to apply monthly interest
/// to all active loans that have an interest rate set.
/// </summary>
public class InterestAccrualBackgroundService(IServiceProvider services, ILogger<InterestAccrualBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AccrueAllAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during interest accrual");
            }

            // Wait until 2 AM the next day
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(2);
            var delay = nextRun - now;
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task AccrueAllAsync()
    {
        using var scope = services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<LoanInterestService>();
        await svc.AccrueAllAsync();
    }
}
