using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411001)]
public class M008_ExpandCashFlowTransactions : Migration
{
    public override void Up()
    {
        Alter.Table("CashFlowTransactions")
            .AddColumn("BankAccountId").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("DebtGroupId").AsInt32().Nullable()
            .AddColumn("IsProjected").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("IsManualOverride").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("GeneratedByStrategy").AsInt32().Nullable()
            .AddColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("IX_CashFlowTransactions_Account_Date")
            .OnTable("CashFlowTransactions")
            .OnColumn("BankAccountId").Ascending()
            .OnColumn("TransactionDate").Ascending();

        Create.Index("IX_CashFlowTransactions_Group_Projected")
            .OnTable("CashFlowTransactions")
            .OnColumn("DebtGroupId").Ascending()
            .OnColumn("IsProjected").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_CashFlowTransactions_Group_Projected").OnTable("CashFlowTransactions");
        Delete.Index("IX_CashFlowTransactions_Account_Date").OnTable("CashFlowTransactions");
        Delete.Column("UpdatedAt").FromTable("CashFlowTransactions");
        Delete.Column("GeneratedByStrategy").FromTable("CashFlowTransactions");
        Delete.Column("IsManualOverride").FromTable("CashFlowTransactions");
        Delete.Column("IsProjected").FromTable("CashFlowTransactions");
        Delete.Column("DebtGroupId").FromTable("CashFlowTransactions");
        Delete.Column("BankAccountId").FromTable("CashFlowTransactions");
    }
}
