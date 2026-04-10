using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410001)]
public class M001_InitialSchema : Migration
{
    public override void Up()
    {
        Create.Table("Debts")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Lender").AsString(200).Nullable()
            .WithColumn("Balance").AsDecimal(18, 2).NotNullable()
            .WithColumn("InterestRate").AsDecimal(10, 6).NotNullable()
            .WithColumn("MinimumPayment").AsDecimal(18, 2).NotNullable()
            .WithColumn("PayoffDate").AsDate().Nullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("Bills")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Frequency").AsInt32().NotNullable()
            .WithColumn("DayDue").AsInt32().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("Loans")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Borrower").AsString(200).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("Principal").AsDecimal(18, 2).NotNullable()
            .WithColumn("InterestRate").AsDecimal(10, 6).NotNullable()
            .WithColumn("AmountRepaid").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("LoanDate").AsDate().NotNullable()
            .WithColumn("ExpectedRepaymentDate").AsDate().Nullable()
            .WithColumn("IsSettled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("CashFlowTransactions")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("TransactionDate").AsDate().NotNullable()
            .WithColumn("Description").AsString(500).NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Category").AsInt32().NotNullable()
            .WithColumn("ReferenceId").AsInt32().Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("CashFlowTransactions");
        Delete.Table("Loans");
        Delete.Table("Bills");
        Delete.Table("Debts");
    }
}
