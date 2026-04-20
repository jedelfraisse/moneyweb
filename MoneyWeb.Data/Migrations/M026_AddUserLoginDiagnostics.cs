using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260416000000)]
public class M026_AddUserLoginDiagnostics : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("LastLoginClaimsJson").AsCustom("NVARCHAR(MAX)").NotNullable().WithDefaultValue(string.Empty)
            .AddColumn("LastLoginAtUtc").AsDateTime().Nullable();
    }

    public override void Down()
    {
        Delete.Column("LastLoginClaimsJson").FromTable("Users");
        Delete.Column("LastLoginAtUtc").FromTable("Users");
    }
}
