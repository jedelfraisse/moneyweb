using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

/// <summary>Lets a recurring debt fee be Monthly, Quarterly, or Yearly instead of implicitly always
/// monthly — issue #15. DueDay/DueMonth anchor when a non-monthly fee actually falls due (DueMonth is
/// shared: for Quarterly it's the first month of the 3-month cycle, for Yearly it's the due month).</summary>
[Migration(20260819000000)]
public class M034_AddDebtFeeFrequency : Migration
{
    public override void Up()
    {
        Alter.Table("DebtFees")
            .AddColumn("Frequency").AsInt32().NotNullable().WithDefaultValue(0) // 0 = Monthly — preserves today's behavior for existing rows
            .AddColumn("DueDay").AsInt32().Nullable()
            .AddColumn("DueMonth").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("DueMonth").FromTable("DebtFees");
        Delete.Column("DueDay").FromTable("DebtFees");
        Delete.Column("Frequency").FromTable("DebtFees");
    }
}
