using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class SharingContactRepository(string connectionString) : ISharingContactRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<SharingContact>> GetAllAsync(int ownerUserId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<SharingContact>("""
            SELECT sc.*, u.DisplayName AS DisplayName
            FROM SharingContacts sc
            LEFT JOIN Users u ON u.Id = sc.LinkedUserId
            WHERE sc.OwnerUserId = @OwnerUserId
            ORDER BY sc.InvitedEmail
            """, new { OwnerUserId = ownerUserId });
    }

    public async Task<SharingContact> AddAsync(int ownerUserId, string email, string? displayName)
    {
        using var conn = Connect();
        // Resolve LinkedUserId if email already belongs to a registered user
        var linkedUserId = await conn.ExecuteScalarAsync<int?>(
            "SELECT Id FROM Users WHERE Email = @Email", new { Email = email });

        var id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO SharingContacts (OwnerUserId, InvitedEmail, LinkedUserId, DisplayName)
            OUTPUT INSERTED.Id
            VALUES (@OwnerUserId, @Email, @LinkedUserId, @DisplayName)
            """,
            new { OwnerUserId = ownerUserId, Email = email, LinkedUserId = linkedUserId, DisplayName = displayName });

        return (await GetAllAsync(ownerUserId)).First(c => c.Id == id);
    }

    public async Task RemoveAsync(int id, int ownerUserId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM SharingContacts WHERE Id = @Id AND OwnerUserId = @OwnerUserId",
            new { Id = id, OwnerUserId = ownerUserId });
    }

    public async Task LinkUserAsync(string email, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE SharingContacts
            SET LinkedUserId = @UserId
            WHERE InvitedEmail = @Email AND LinkedUserId IS NULL
            """, new { UserId = userId, Email = email });
    }
}
