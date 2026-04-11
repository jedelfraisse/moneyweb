using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IShareRepository
{
    /// <summary>Returns all shares for a specific entity (e.g., all shares on Debt #5).</summary>
    Task<IEnumerable<SharePermission>> GetSharesForEntityAsync(ShareEntityType entityType, int entityId);

    /// <summary>Returns entity IDs of the given type owned by OTHER users that are shared with userId.</summary>
    Task<IEnumerable<(int EntityId, int OwnerUserId, string OwnerDisplayName)>> GetSharedWithMeAsync(int userId, ShareEntityType entityType);

    /// <summary>Returns all approved users visible to the given user (contacts via groups + all approved users).</summary>
    Task<IEnumerable<(int UserId, string DisplayName)>> GetShareableUsersAsync(int currentUserId);

    Task AddShareAsync(SharePermission permission);
    Task RemoveShareAsync(int shareId, int grantedByUserId);
}
