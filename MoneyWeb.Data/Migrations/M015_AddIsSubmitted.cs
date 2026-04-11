using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411008)]
public class M015_AddIsSubmitted : Migration
{
    public override void Up()
    {
        Alter.Table("CashFlowTransactions")
            .AddColumn("IsSubmitted").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("IsSubmitted").FromTable("CashFlowTransactions");
    }
}
