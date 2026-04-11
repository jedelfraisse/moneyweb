using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IUserGroupRepository
{
    Task<IEnumerable<UserGroup>> GetAllForUserAsync(int userId);
    Task<UserGroup?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(UserGroup group);
    Task UpdateAsync(UserGroup group);
    Task DeleteAsync(int id, int ownerId);
    Task InviteMemberAsync(int groupId, int ownerId, string email);
    Task UpdateMemberCanSeeAllAsync(int memberId, int groupId, int ownerId, bool canSeeAll);
    Task RemoveMemberAsync(int memberId, int groupId, int ownerId);
    Task CheckAndJoinPendingInvitesAsync(int userId, string email);
}
