using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>Lets a recurring debt fee (e.g. property tax, HOA) be earmarked to a specific
/// Sinking Fund the user is saving into for it — issue #17.</summary>
[Migration(20260818000000)]
public class M033_AddDebtFeeSinkingFund : Migration
{
    public override void Up()
    {
        Alter.Table("DebtFees").AddColumn("SinkingFundId").AsInt32().Nullable();

        // SetNull rather than a hard block — deleting the sinking fund shouldn't be prevented by,
        // or cascade into deleting, the fee that referenced it; it just un-links.
        Create.ForeignKey("FK_DebtFees_SinkingFunds")
            .FromTable("DebtFees").ForeignColumn("SinkingFundId")
            .ToTable("SinkingFunds").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);

        Create.Index("IX_DebtFees_SinkingFundId").OnTable("DebtFees").OnColumn("SinkingFundId");
    }

    public override void Down()
    {
        Delete.Index("IX_DebtFees_SinkingFundId").OnTable("DebtFees");
        Delete.ForeignKey("FK_DebtFees_SinkingFunds").OnTable("DebtFees");
        Delete.Column("SinkingFundId").FromTable("DebtFees");
    }
}
