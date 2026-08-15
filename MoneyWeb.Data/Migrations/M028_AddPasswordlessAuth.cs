using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260814000000)]
public class M028_AddPasswordlessAuth : Migration
{
    public override void Up()
    {
        Create.Table("LoginTokens")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Email").AsString(256).NotNullable()
            .WithColumn("TokenHash").AsString(128).NotNullable()
            .WithColumn("CodeHash").AsString(128).NotNullable()
            .WithColumn("AttemptCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("ConsumedAtUtc").AsDateTime2().Nullable()
            .WithColumn("ExpiresAtUtc").AsDateTime2().NotNullable()
            .WithColumn("CreatedAtUtc").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("IX_LoginTokens_Email").OnTable("LoginTokens").OnColumn("Email");
        Create.Index("UX_LoginTokens_TokenHash").OnTable("LoginTokens").OnColumn("TokenHash").Unique();

        // EntraObjectId stops being the identity anchor now that login is passwordless —
        // made nullable, and its old unique index dropped (unlike most databases, SQL Server
        // allows only ONE null in a unique index, not many — so multiple new passwordless
        // users with no EntraObjectId would collide on that constraint). Left in place as a
        // plain nullable column for historical/diagnostic value on existing rows.
        Delete.Index("IX_Users_EntraObjectId").OnTable("Users");
        Alter.Column("EntraObjectId").OnTable("Users").AsString(36).Nullable();

        // Email becomes the new identity anchor.
        Create.Index("UX_Users_Email").OnTable("Users").OnColumn("Email").Unique();
    }

    public override void Down()
    {
        Delete.Index("UX_Users_Email").OnTable("Users");
        Alter.Column("EntraObjectId").OnTable("Users").AsString(36).NotNullable();
        Create.Index("IX_Users_EntraObjectId").OnTable("Users").OnColumn("EntraObjectId").Unique();
        Delete.Table("LoginTokens");
    }
}
