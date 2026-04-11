using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ICashFlowRepository
{
    /// <summary>All transactions for an account within the date range, sorted by date.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetForAccountAsync(int bankAccountId, int userId, DateOnly from, DateOnly to);

    /// <summary>Delete all projected transactions for a specific source (Income, Bill, Loan) by ReferenceId and Category.</summary>
    Task DeleteProjectedForSourceAsync(int referenceId, int userId, TransactionCategory category);

    /// <summary>Count of projected transactions for a debt group that the user has manually overridden.</summary>
    Task<int> CountManualOverridesAsync(int debtGroupId, int userId);

    /// <summary>Delete all projected (system-generated) transactions for a debt group. When includeManualOverrides is false, manual overrides are preserved.</summary>
    Task DeleteProjectedForGroupAsync(int debtGroupId, int userId, bool includeManualOverrides);

    /// <summary>Insert a batch of projected transactions (generated from payoff simulation).</summary>
    Task BulkInsertProjectedAsync(IEnumerable<CashFlowTransaction> transactions);

    /// <summary>Update the date of a single transaction, marking it as a manual override.</summary>
    Task UpdateManualOverrideDateAsync(int id, int userId, DateOnly newDate);

    /// <summary>Mark a projected transaction as confirmed/processed (IsProjected = false).</summary>
    Task MarkAsProcessedAsync(int id, int userId);

    /// <summary>Delete a single transaction (skip/remove from cash flow).</summary>
    Task DeleteAsync(int id, int userId);
}
