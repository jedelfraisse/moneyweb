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
}
