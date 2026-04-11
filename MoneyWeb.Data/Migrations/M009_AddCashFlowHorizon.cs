using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411002)]
public class M009_AddCashFlowHorizon : Migration
{
    public override void Up()
    {
        Alter.Table("Users")
            .AddColumn("CashFlowHorizonMonths").AsInt32().NotNullable().WithDefaultValue(12);
    }

    public override void Down()
    {
        Delete.Column("CashFlowHorizonMonths").FromTable("Users");
    }
}
