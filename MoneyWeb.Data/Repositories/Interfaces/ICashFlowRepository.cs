using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ICashFlowRepository
{
    /// <summary>All transactions for an account within the date range, sorted by date.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetForAccountAsync(int bankAccountId, int userId, DateOnly from, DateOnly to);

    /// <summary>Delete all projected transactions for a specific source (Income, Bill, Loan) by ReferenceId and Category.</summary>
    Task DeleteProjectedForSourceAsync(int referenceId, int userId, TransactionCategory category);

    /// <summary>Delete projected transactions for a source up to and including a specific date (used when recording an actual bill).</summary>
    Task DeleteProjectedForSourceUpToDateAsync(int referenceId, int userId, TransactionCategory category, DateOnly upToDate);

    /// <summary>Count of projected transactions for a debt group that the user has manually overridden.</summary>
    Task<int> CountManualOverridesAsync(int debtGroupId, int userId);

    /// <summary>Delete all projected (system-generated) transactions for a debt group. When includeManualOverrides is false, manual overrides are preserved.</summary>
    Task DeleteProjectedForGroupAsync(int debtGroupId, int userId, bool includeManualOverrides);

    /// <summary>Insert a batch of projected transactions (generated from payoff simulation).</summary>
    Task BulkInsertProjectedAsync(IEnumerable<CashFlowTransaction> transactions);

    /// <summary>Update the date of a single transaction, marking it as a manual override.</summary>
    Task UpdateManualOverrideDateAsync(int id, int userId, DateOnly newDate);

    /// <summary>Mark a projected transaction as submitted to the vendor — awaiting clearance.</summary>
    Task MarkAsSubmittedAsync(int id, int userId, DateOnly submittedDate);

    /// <summary>Mark a projected transaction as confirmed/processed (IsProjected = false).</summary>
    Task MarkAsProcessedAsync(int id, int userId);

    /// <summary>Insert a user-created manual transaction (not tied to any debt or bill).</summary>
    Task InsertManualAsync(CashFlowTransaction t);

    /// <summary>Insert a user-scheduled projected payment (IsManualOverride = true, so strategy regeneration won't delete it).</summary>
    Task InsertScheduledPaymentAsync(CashFlowTransaction t);

    /// <summary>Delete a single transaction (skip/remove from cash flow).</summary>
    Task DeleteAsync(int id, int userId);

    /// <summary>Distinct ReferenceIds that have at least one projected transaction for the given category.</summary>
    Task<IEnumerable<int>> GetProjectedSourceIdsAsync(int userId, TransactionCategory category);

    /// <summary>Distinct DebtGroupIds that have at least one projected transaction.</summary>
    Task<IEnumerable<int>> GetProjectedGroupIdsAsync(int userId);

    /// <summary>Update the amount on all future projected transactions for a given source (e.g. after recording an actual bill amount).</summary>
    Task UpdateProjectedAmountsAsync(int referenceId, int userId, TransactionCategory category, decimal newAmount, DateOnly fromDate);

    /// <summary>Returns true if a projected (non-manual-override) transaction already exists for the given source on the specified date.</summary>
    Task<bool> HasProjectedForSourceOnDateAsync(int referenceId, int userId, TransactionCategory category, DateOnly date);

    /// <summary>All projected transactions for a user and category, ordered by date. Used for pending payments views.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetProjectedForCategoryAsync(int userId, TransactionCategory category);

    /// <summary>Delete all manual-override projected transactions for a source on a specific date (e.g. when deleting a recorded bill occurrence).</summary>
    Task DeleteManualProjectedOnDateAsync(int referenceId, int userId, TransactionCategory category, DateOnly date);
}
