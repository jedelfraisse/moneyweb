using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteUserDataAsync(int userId);  // deletes all domain rows + user row
}
