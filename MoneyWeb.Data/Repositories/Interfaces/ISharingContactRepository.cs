using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ISharingContactRepository
{
    Task<IEnumerable<SharingContact>> GetAllAsync(int ownerUserId);
    Task<SharingContact> AddAsync(int ownerUserId, string email, string? displayName);
    Task RemoveAsync(int id, int ownerUserId);
    /// <summary>Called on login — links the user's account to any contacts matching their email.</summary>
    Task LinkUserAsync(string email, int userId);
}
