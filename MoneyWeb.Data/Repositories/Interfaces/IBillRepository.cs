using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IBillRepository
{
    Task<IEnumerable<Bill>> GetAllAsync(int userId, bool activeOnly = true);
    Task<Bill?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Bill bill);
    Task UpdateAsync(Bill bill);
    Task DeleteAsync(int id, int userId);

    // Occurrences
    Task<IEnumerable<BillOccurrence>> GetOccurrencesAsync(int billId, int userId);
    Task<IEnumerable<BillOccurrence>> GetUpcomingOccurrencesAsync(int userId, DateOnly from, DateOnly to);
    Task<IEnumerable<BillOccurrence>> GetOpenOccurrencesAsync(int userId);
    Task<int> CreateOccurrenceAsync(BillOccurrence occurrence);
    Task UpdateOccurrenceAsync(BillOccurrence occurrence);
    Task DeleteOccurrenceAsync(int id, int userId);

    /// <summary>Delete an open bill occurrence that matches the bill and its effective pay date (PlannedPayDate if set, otherwise DueDate).</summary>
    Task DeleteOccurrenceByBillAndPayDateAsync(int billId, int userId, DateOnly payDate);

    /// <summary>Update the status (and optionally submitted date) of a bill occurrence matched by bill + effective pay date.
    /// Pass <paramref name="newPlannedPayDate"/> when the cash-flow transaction is being moved, so the two stay matched.</summary>
    Task UpdateOccurrenceStatusByBillAndPayDateAsync(int billId, int userId, DateOnly payDate, BillOccurrenceStatus status, DateOnly? submittedDate, DateOnly? newPlannedPayDate = null);

    /// <summary>A bill's cash-flow payment cleared — mark its matching open occurrence Confirmed, or record a new
    /// Confirmed occurrence if there wasn't one (e.g. an auto-draft), so the bill's next due date advances.</summary>
    Task ConfirmPaymentByBillAndPayDateAsync(int billId, int userId, DateOnly payDate, decimal amount);

    /// <summary>Ids of open occurrences for bills linked to a bank account that have no matching cash-flow
    /// transaction — usually left behind when the payment was cleared or skipped on the Transactions page.</summary>
    Task<IEnumerable<int>> GetOpenOccurrenceIdsMissingCashFlowAsync(int userId);
}
