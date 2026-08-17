using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>
/// Grants admin (and approved) status to a second default admin account. Idempotent — safe to re-run,
/// and works whether or not the user has signed in yet (Email is the identity anchor since passwordless
/// auth landed in M028; see PasswordlessAuthService for the shape a first-login row would otherwise take).
/// </summary>
[Migration(20260816000000)]
public class M031_AddDefaultAdmin : Migration
{
    private const string AdminEmail = "tw.ringo@gmail.com";

    public override void Up()
    {
        Execute.Sql($"""
            IF EXISTS (SELECT 1 FROM Users WHERE Email = '{AdminEmail}')
                UPDATE Users SET IsAdmin = 1, IsApproved = 1 WHERE Email = '{AdminEmail}'
            ELSE
                INSERT INTO Users (Email, DisplayName, IsApproved, IsAdmin, CreatedAt)
                VALUES ('{AdminEmail}', '{AdminEmail}', 1, 1, GETUTCDATE())
            """);
    }

    public override void Down()
    {
        // Intentionally not reverted — demoting an admin on migration rollback is more surprising than
        // helpful, and rollbacks of this migration aren't expected in practice.
    }
}
