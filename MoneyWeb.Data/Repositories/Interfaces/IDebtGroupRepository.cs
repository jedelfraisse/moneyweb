using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IDebtGroupRepository
{
    Task<IEnumerable<DebtGroup>> GetAllAsync(int userId);
    Task<DebtGroup?> GetByIdAsync(int id, int userId);
    /// <summary>Returns the group with its Debts and BankAccount populated.</summary>
    Task<DebtGroup?> GetWithDebtsAsync(int id, int userId);
    Task<IEnumerable<DebtGroup>> GetAllWithDebtsAsync(int userId);
    Task<int> CreateAsync(DebtGroup group);
    Task UpdateAsync(DebtGroup group);
    Task DeleteAsync(int id, int userId);
}
