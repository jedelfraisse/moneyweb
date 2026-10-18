using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>The bank account the dashboard's daily balance chart opens on.</summary>
[Migration(20261018000000)]
public class M037_AddUserDashboardAccount : Migration
{
    public override void Up()
    {
        Alter.Table("Users").AddColumn("DashboardAccountId").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("DashboardAccountId").FromTable("Users");
    }
}
