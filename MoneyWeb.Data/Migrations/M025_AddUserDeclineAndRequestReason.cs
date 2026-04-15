using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260415000000)]
public class M025_AddUserDeclineAndRequestReason : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("IsDeclined").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("DeclineReason").AsString(500).NotNullable().WithDefaultValue("")
            .AddColumn("RequestReason").AsString(500).NotNullable().WithDefaultValue("");
    }

    public override void Down()
    {
        Delete.Column("IsDeclined").FromTable("Users");
        Delete.Column("DeclineReason").FromTable("Users");
        Delete.Column("RequestReason").FromTable("Users");
    }
}
