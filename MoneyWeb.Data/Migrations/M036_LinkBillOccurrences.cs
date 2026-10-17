using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>Ties a cash-flow payment to the exact bill occurrence it pays (one-offs had no link at all — they have
/// no BillId), and lets a bill occurrence record the sinking fund that saved for it.</summary>
[Migration(20261017000000)]
public class M036_LinkBillOccurrences : Migration
{
    public override void Up()
    {
        Alter.Table("CashFlowTransactions").AddColumn("BillOccurrenceId").AsInt32().Nullable();
        Create.Index("IX_CashFlowTransactions_BillOccurrenceId").OnTable("CashFlowTransactions").OnColumn("BillOccurrenceId");
        Alter.Table("BillOccurrences").AddColumn("SinkingFundId").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Index("IX_CashFlowTransactions_BillOccurrenceId").OnTable("CashFlowTransactions");
        Delete.Column("BillOccurrenceId").FromTable("CashFlowTransactions");
        Delete.Column("SinkingFundId").FromTable("BillOccurrences");
    }
}
