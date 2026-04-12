using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411014)]
public class M019_AddFeatureToggles : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("ShowSinkingFunds").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("ShowLoans").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("ShowFamily").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("ShowSinkingFunds").FromTable("Users");
        Delete.Column("ShowLoans").FromTable("Users");
        Delete.Column("ShowFamily").FromTable("Users");
    }
}
