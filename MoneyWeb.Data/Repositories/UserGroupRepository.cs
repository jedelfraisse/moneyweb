using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class UserGroupRepository(string connectionString) : IUserGroupRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<UserGroup>> GetAllForUserAsync(int userId)
    {
        using var conn = Connect();
        var groups = (await conn.QueryAsync<UserGroup>("""
            SELECT g.*, u.DisplayName AS OwnerDisplayName
            FROM UserGroups g
            INNER JOIN Users u ON u.Id = g.OwnerId
            WHERE g.OwnerId = @UserId
               OR EXISTS (
                   SELECT 1 FROM UserGroupMembers m
                   WHERE m.GroupId = g.Id AND m.UserId = @UserId AND m.Status = 1
               )
            ORDER BY g.Name
            """, new { UserId = userId })).ToList();

        foreach (var group in groups)
        {
            group.Members = (await conn.QueryAsync<UserGroupMember>("""
                SELECT m.*, u.DisplayName
                FROM UserGroupMembers m
                LEFT JOIN Users u ON u.Id = m.UserId
                WHERE m.GroupId = @GroupId
                ORDER BY m.InvitedEmail
                """, new { GroupId = group.Id })).ToList();
        }

        return groups;
    }

    public async Task<UserGroup?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        var group = await conn.QuerySingleOrDefaultAsync<UserGroup>("""
            SELECT g.*, u.DisplayName AS OwnerDisplayName
            FROM UserGroups g
            INNER JOIN Users u ON u.Id = g.OwnerId
            WHERE g.Id = @Id
              AND (
                g.OwnerId = @UserId
                OR EXISTS (
                    SELECT 1 FROM UserGroupMembers m
                    WHERE m.GroupId = g.Id AND m.UserId = @UserId AND m.Status = 1
                )
              )
            """, new { Id = id, UserId = userId });

        if (group is null) return null;

        group.Members = (await conn.QueryAsync<UserGroupMember>("""
            SELECT m.*, u.DisplayName
            FROM UserGroupMembers m
            LEFT JOIN Users u ON u.Id = m.UserId
            WHERE m.GroupId = @GroupId
            ORDER BY m.InvitedEmail
            """, new { GroupId = group.Id })).ToList();

        return group;
    }

    public async Task<int> CreateAsync(UserGroup group)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            var groupId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO UserGroups (OwnerId, Name, Notes, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.Id
                VALUES (@OwnerId, @Name, @Notes, GETUTCDATE(), GETUTCDATE())
                """, group, tx);

            await conn.ExecuteAsync("""
                INSERT INTO UserGroupMembers (GroupId, UserId, InvitedEmail, Status, CanSeeAll, InvitedAt, JoinedAt)
                SELECT @GroupId, Id, Email, 1, 1, GETUTCDATE(), GETUTCDATE()
                FROM Users WHERE Id = @OwnerId
                """, new { GroupId = groupId, OwnerId = group.OwnerId }, tx);

            await tx.CommitAsync();
            return groupId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(UserGroup group)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE UserGroups SET
                Name = @Name, Notes = @Notes, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND OwnerId = @OwnerId
            """, group);
    }

    public async Task DeleteAsync(int id, int ownerId)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync(
                "DELETE FROM UserGroupMembers WHERE GroupId = @Id",
                new { Id = id }, tx);
            await conn.ExecuteAsync(
                "DELETE FROM UserGroups WHERE Id = @Id AND OwnerId = @OwnerId",
                new { Id = id, OwnerId = ownerId }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task InviteMemberAsync(int groupId, int ownerId, string email)
    {
        using var conn = Connect();
        var ownerCheck = await conn.ExecuteScalarAsync<int?>(
            "SELECT OwnerId FROM UserGroups WHERE Id = @GroupId",
            new { GroupId = groupId });

        if (ownerCheck != ownerId)
            throw new UnauthorizedAccessException("Only the group owner can invite members.");

        // Check if already a member (by email)
        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM UserGroupMembers WHERE GroupId = @GroupId AND InvitedEmail = @Email",
            new { GroupId = groupId, Email = email });
        if (existing > 0) return;

        var foundUserId = await conn.ExecuteScalarAsync<int?>(
            "SELECT Id FROM Users WHERE Email = @Email",
            new { Email = email });

        if (foundUserId.HasValue)
        {
            await conn.ExecuteAsync("""
                INSERT INTO UserGroupMembers (GroupId, UserId, InvitedEmail, Status, CanSeeAll, InvitedAt, JoinedAt)
                VALUES (@GroupId, @UserId, @Email, 1, 0, GETUTCDATE(), GETUTCDATE())
                """, new { GroupId = groupId, UserId = foundUserId.Value, Email = email });

            await conn.ExecuteAsync(
                "UPDATE Users SET IsApproved = 1 WHERE Id = @UserId",
                new { UserId = foundUserId.Value });
        }
        else
        {
            await conn.ExecuteAsync("""
                INSERT INTO UserGroupMembers (GroupId, UserId, InvitedEmail, Status, CanSeeAll, InvitedAt)
                VALUES (@GroupId, NULL, @Email, 0, 0, GETUTCDATE())
                """, new { GroupId = groupId, Email = email });
        }
    }

    public async Task UpdateMemberCanSeeAllAsync(int memberId, int groupId, int ownerId, bool canSeeAll)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE UserGroupMembers SET CanSeeAll = @CanSeeAll
            WHERE Id = @MemberId AND GroupId = @GroupId
              AND EXISTS (SELECT 1 FROM UserGroups WHERE Id = @GroupId AND OwnerId = @OwnerId)
            """, new { MemberId = memberId, GroupId = groupId, OwnerId = ownerId, CanSeeAll = canSeeAll });
    }

    public async Task RemoveMemberAsync(int memberId, int groupId, int ownerId)
    {
        using var conn = Connect();
        // Don't allow removing the owner's own membership row
        await conn.ExecuteAsync("""
            DELETE FROM UserGroupMembers
            WHERE Id = @MemberId AND GroupId = @GroupId
              AND EXISTS (SELECT 1 FROM UserGroups WHERE Id = @GroupId AND OwnerId = @OwnerId)
              AND UserId != @OwnerId
            """, new { MemberId = memberId, GroupId = groupId, OwnerId = ownerId });
    }

    public async Task CheckAndJoinPendingInvitesAsync(int userId, string email)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE UserGroupMembers SET UserId = @UserId, Status = 1, JoinedAt = GETUTCDATE()
            WHERE InvitedEmail = @Email AND Status = 0
            """, new { UserId = userId, Email = email });

        await conn.ExecuteAsync("""
            UPDATE Users SET IsApproved = 1 WHERE Id = @UserId
            AND EXISTS (SELECT 1 FROM UserGroupMembers WHERE UserId = @UserId AND Status = 1)
            """, new { UserId = userId });
    }
}
