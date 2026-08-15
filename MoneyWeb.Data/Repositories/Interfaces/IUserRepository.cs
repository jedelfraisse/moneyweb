using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IUserRepository
{
    /// <summary>Legacy lookup by the old Entra identity anchor — kept for historical/diagnostic use only.</summary>
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteUserDataAsync(int userId);  // deletes all domain rows + user row
}
