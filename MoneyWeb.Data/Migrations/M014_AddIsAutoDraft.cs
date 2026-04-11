using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411007)]
public class M014_AddIsAutoDraft : Migration
{
    public override void Up()
    {
        Alter.Table("CashFlowTransactions")
            .AddColumn("IsAutoDraft").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    public override void Down()
    {
        Delete.Column("IsAutoDraft").FromTable("CashFlowTransactions");
    }
}
