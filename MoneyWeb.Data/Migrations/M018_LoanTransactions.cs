using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411013)]
public class M018_LoanTransactions : Migration
{
    public override void Up()
    {
        Alter.Table("Loans")
            .AddColumn("Email").AsString(200).Nullable()
            .AddColumn("Phone").AsString(50).Nullable();

        Create.Table("LoanTransactions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("LoanId").AsInt32().NotNullable()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("TransactionDate").AsDate().NotNullable()
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Notes").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("LoanTransactions");
        Delete.Column("Email").FromTable("Loans");
        Delete.Column("Phone").FromTable("Loans");
    }
}
