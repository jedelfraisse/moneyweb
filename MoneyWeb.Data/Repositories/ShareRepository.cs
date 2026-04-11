using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class ShareRepository(string connectionString) : IShareRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<SharePermission>> GetSharesForEntityAsync(ShareEntityType entityType, int entityId)
    {
        using var conn = Connect();
        var sql = """
            SELECT sp.*, 
                   COALESCE(u.DisplayName, ug.Name) AS SharedWithDisplayName
            FROM SharePermissions sp
            LEFT JOIN Users u ON u.Id = sp.SharedWithUserId
            LEFT JOIN UserGroups ug ON ug.Id = sp.SharedWithGroupId
            WHERE sp.EntityType = @EntityType AND sp.EntityId = @EntityId
            ORDER BY sp.CreatedAt
            """;
        return await conn.QueryAsync<SharePermission>(sql,
            new { EntityType = (int)entityType, EntityId = entityId });
    }

    public async Task<IEnumerable<(int EntityId, int OwnerUserId, string OwnerDisplayName)>> GetSharedWithMeAsync(
        int userId, ShareEntityType entityType)
    {
        using var conn = Connect();
        var sql = """
            SELECT DISTINCT sp.EntityId, u.Id AS OwnerUserId, u.DisplayName AS OwnerDisplayName
            FROM SharePermissions sp
            INNER JOIN Users u ON u.Id = sp.GrantedByUserId
            WHERE sp.EntityType = @EntityType
              AND sp.GrantedByUserId != @UserId
              AND (
                sp.SharedWithUserId = @UserId
                OR (sp.SharedWithGroupId IS NOT NULL AND EXISTS (
                    SELECT 1 FROM UserGroupMembers m
                    WHERE m.GroupId = sp.SharedWithGroupId
                      AND m.UserId = @UserId
                      AND m.Status = 1
                ))
              )
            """;
        var rows = await conn.QueryAsync(sql,
            new { EntityType = (int)entityType, UserId = userId });
        return rows.Select(r => ((int)r.EntityId, (int)r.OwnerUserId, (string)r.OwnerDisplayName));
    }

    public async Task<IEnumerable<(int UserId, string DisplayName)>> GetShareableUsersAsync(int currentUserId)
    {
        using var conn = Connect();
        // All approved users except self, ordered by display name
        var sql = """
            SELECT Id AS UserId, DisplayName
            FROM Users
            WHERE IsApproved = 1 AND Id != @CurrentUserId
            ORDER BY DisplayName
            """;
        var rows = await conn.QueryAsync(sql, new { CurrentUserId = currentUserId });
        return rows.Select(r => ((int)r.UserId, (string)r.DisplayName));
    }

    public async Task AddShareAsync(SharePermission permission)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            INSERT INTO SharePermissions (EntityType, EntityId, GrantedByUserId, SharedWithUserId, SharedWithGroupId, CreatedAt)
            VALUES (@EntityType, @EntityId, @GrantedByUserId, @SharedWithUserId, @SharedWithGroupId, GETUTCDATE())
            """,
            new
            {
                EntityType = (int)permission.EntityType,
                permission.EntityId,
                permission.GrantedByUserId,
                permission.SharedWithUserId,
                permission.SharedWithGroupId
            });
    }

    public async Task RemoveShareAsync(int shareId, int grantedByUserId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM SharePermissions WHERE Id = @Id AND GrantedByUserId = @GrantedByUserId",
            new { Id = shareId, GrantedByUserId = grantedByUserId });
    }
}
