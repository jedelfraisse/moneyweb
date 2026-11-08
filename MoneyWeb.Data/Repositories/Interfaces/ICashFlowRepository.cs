using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ICashFlowRepository
{
    /// <summary>All transactions for an account within the date range, sorted by date.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetForAccountAsync(int bankAccountId, int userId, DateOnly from, DateOnly to);

    /// <summary>Transactions for an account dated strictly before a cutoff — used to surface stale pending
    /// items that fall outside the cash-flow view's normal lookback window.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetForAccountBeforeAsync(int bankAccountId, int userId, DateOnly before);

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

    /// <summary>Mark a projected transaction as submitted to the vendor — awaiting clearance — and set its
    /// expected payment date, and optionally its amount (signed, as stored). Also pins it as a manual override so a
    /// later projection re-push won't drop it.</summary>
    Task MarkAsSubmittedAsync(int id, int userId, DateOnly expectedPayDate, decimal? newAmount = null);

    /// <summary>Undo a submitted transaction — back to a plain pending projection (IsSubmitted = 0).</summary>
    Task MarkAsUnsubmittedAsync(int id, int userId);

    /// <summary>Mark a projected transaction as confirmed/processed (IsProjected = false).</summary>
    Task MarkAsProcessedAsync(int id, int userId);

    /// <summary>Insert a user-created manual transaction (not tied to any debt or bill).</summary>
    Task InsertManualAsync(CashFlowTransaction t);

    /// <summary>Insert a user-scheduled projected payment (IsManualOverride = true, so strategy regeneration won't delete it).</summary>
    Task InsertScheduledPaymentAsync(CashFlowTransaction t);

    /// <summary>Delete a single transaction (skip/remove from cash flow).</summary>
    Task DeleteAsync(int id, int userId);

    /// <summary>All transactions generated for one source (e.g. a bill), across every account and date.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetForSourceAsync(int referenceId, int userId, TransactionCategory category);

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

    /// <summary>Every pending (not yet cleared/skipped) transaction dated before a cutoff, across all of the user's accounts.</summary>
    Task<IEnumerable<CashFlowTransaction>> GetPendingBeforeAsync(int userId, DateOnly before);

    /// <summary>Set or clear "submitted" on the pending payment linked to a bill occurrence (by BillOccurrenceId),
    /// optionally moving its date and changing its amount (a positive payment amount; stored as an outflow).
    /// Pins it as a manual override so regeneration leaves it alone.</summary>
    Task SetSubmittedForOccurrenceAsync(int billOccurrenceId, int userId, bool submitted, DateOnly? newDate = null,
        decimal? newPaymentAmount = null);

    /// <summary>Delete the pending payment(s) linked to a bill occurrence (by BillOccurrenceId).</summary>
    Task DeleteForOccurrenceAsync(int billOccurrenceId, int userId);

    /// <summary>
    /// A bill occurrence was edited: move its pending payment to the new pay date and, when given, set its amount
    /// (a positive payment amount; stored as an outflow). Finds the payment by BillOccurrenceId, or for older rows by
    /// bill + the occurrence's previous pay date. Without this the payment is left on the old date, the occurrence
    /// shows "Not in cash flow", and adding it again duplicates it.
    /// </summary>
    Task MoveOccurrencePaymentAsync(int billOccurrenceId, int? billId, int userId, DateOnly oldPayDate, DateOnly newPayDate,
        decimal? newPaymentAmount);

    /// <summary>Mark a bill occurrence's CF transaction as submitted (awaiting clearance), matched by
    /// source+date rather than by ID. Does not move the transaction's date — "submitted" is a status,
    /// not a reschedule; the payment still lands on its planned date. Optionally changes its amount
    /// (a positive payment amount; stored as an outflow).</summary>
    Task MarkSubmittedForSourceOnDateAsync(int referenceId, int userId, TransactionCategory category, DateOnly date,
        decimal? newPaymentAmount = null);

    /// <summary>
    /// Delete every still-upcoming (IsProjected) transaction for a source, regardless of manual-override —
    /// used when the source itself (e.g. a Bill) is being deleted and its future transactions should go
    /// with it. Already-cleared history (IsProjected = false) is left untouched.
    /// </summary>
    Task DeleteAllProjectedForSourceAsync(int referenceId, int userId, TransactionCategory category);

    /// <summary>
    /// Detach every still-upcoming transaction for a source from that source (ReferenceId = NULL) and mark
    /// it a manual override, so it survives as a standalone entry instead of dangling on a deleted source —
    /// used when the source (e.g. a Bill) is deleted but its future transactions should be kept.
    /// </summary>
    Task DetachSourceKeepAsManualAsync(int referenceId, int userId, TransactionCategory category);

    /// <summary>
    /// Admin maintenance: deletes still-upcoming (IsProjected) transactions on a bank account that no longer exists, and
    /// DebtPayment/Bill transactions whose ReferenceId no longer matches any existing Debt/Bill — leftovers from deletes made before both
    /// pages started resolving their cash-flow rows at delete time. Historical (cleared) rows are left
    /// alone. Returns the number of rows removed.
    /// </summary>
    Task<int> DeleteOrphanedProjectedTransactionsAsync();

    /// <summary>Dry-run count for DeleteOrphanedProjectedTransactionsAsync — same criteria, no deletion.
    /// Lets the admin see what a cleanup run would affect before committing to it.</summary>
    Task<int> CountOrphanedProjectedTransactionsAsync();
}
