using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410006)]
public class M006_AddIsFixedPayment : Migration
{
    public override void Up()
    {
        Alter.Table("Debts")
            .AddColumn("IsFixedPayment").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("IsFixedPayment").FromTable("Debts");
    }
}
