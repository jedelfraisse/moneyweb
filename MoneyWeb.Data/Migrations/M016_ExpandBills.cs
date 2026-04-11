using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411011)]
public class M016_ExpandBills : Migration
{
    public override void Up()
    {
        // Expand Bills table (UserId already added in M002)
        Alter.Table("Bills")
            .AddColumn("Category").AsInt32().NotNullable().WithDefaultValue(0)
            .AddColumn("PaymentMethod").AsInt32().NotNullable().WithDefaultValue(1)   // 1 = Manual
            .AddColumn("BankAccountId").AsInt32().Nullable()
            .AddColumn("Notes").AsString(500).Nullable()
            .AddColumn("AnnualMonth").AsInt32().Nullable();                            // 1-12 for Annual bills

        // BillOccurrences — one row per billing period, tracks estimated vs actual
        Create.Table("BillOccurrences")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("BillId").AsInt32().NotNullable()
                .ForeignKey("FK_BillOccurrences_Bills", "Bills", "Id")
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("DueDate").AsDate().NotNullable()
            .WithColumn("EstimatedAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("ActualAmount").AsDecimal(18, 2).Nullable()
            .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(0)          // 0=Estimated
            .WithColumn("SubmittedDate").AsDate().Nullable()
            .WithColumn("Notes").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("IX_BillOccurrences_BillId").OnTable("BillOccurrences").OnColumn("BillId");
        Create.Index("IX_BillOccurrences_UserId_DueDate").OnTable("BillOccurrences")
            .OnColumn("UserId").Ascending()
            .OnColumn("DueDate").Ascending();
    }

    public override void Down()
    {
        Delete.Table("BillOccurrences");
        Delete.Column("AnnualMonth").FromTable("Bills");
        Delete.Column("Notes").FromTable("Bills");
        Delete.Column("BankAccountId").FromTable("Bills");
        Delete.Column("PaymentMethod").FromTable("Bills");
        Delete.Column("Category").FromTable("Bills");
    }
}
