using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260530000000)]
public class M027_AddDebtTypeAndCreditLimit : Migration
{
    public override void Up()
    {
        Alter.Table("Debts")
            .AddColumn("DebtType").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("CreditLimit").AsDecimal(18, 2).Nullable();
    }

    public override void Down()
    {
        Delete.Column("DebtType").FromTable("Debts");
        Delete.Column("CreditLimit").FromTable("Debts");
    }
}
