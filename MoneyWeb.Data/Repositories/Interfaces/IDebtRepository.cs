using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IDebtRepository
{
    Task<IEnumerable<Debt>> GetAllAsync(int userId, bool activeOnly = true);
    Task<Debt?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Debt debt);
    Task UpdateAsync(Debt debt);
    Task DeleteAsync(int id, int userId);
    Task UpdateLastPaymentAsync(int id, int userId, DateOnly paymentDate);
    Task<IEnumerable<Debt>> GetSharedWithMeAsync(int userId);
    Task<IEnumerable<DebtFee>> GetFeesAsync(int debtId, int userId);
    Task<int> CreateFeeAsync(DebtFee fee);
    Task UpdateFeeAsync(DebtFee fee);
    Task DeleteFeeAsync(int id, int userId);
}
