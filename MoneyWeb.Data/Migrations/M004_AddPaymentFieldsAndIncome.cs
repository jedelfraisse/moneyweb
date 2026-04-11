using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410004)]
public class M004_AddPaymentFieldsAndIncome : Migration
{
    public override void Up()
    {
        Alter.Table("Debts")
            .AddColumn("PaymentDayOfMonth").AsInt32().Nullable()
            .AddColumn("LastPaymentDate").AsDate().Nullable()
            .AddColumn("PaymentMethod").AsInt32().NotNullable().WithDefaultValue(1); // 1=Manual

        Create.Table("Income")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("BankAccountId").AsInt32().Nullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("IncomeType").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("IsVariable").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("EstimatedAmount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("Frequency").AsInt32().NotNullable().WithDefaultValue(3)
            .WithColumn("NextPaymentDate").AsDate().NotNullable()
            .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_Income_Users")
            .FromTable("Income").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_Income_BankAccounts")
            .FromTable("Income").ForeignColumn("BankAccountId")
            .ToTable("BankAccounts").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Income_BankAccounts").OnTable("Income");
        Delete.ForeignKey("FK_Income_Users").OnTable("Income");
        Delete.Table("Income");

        Delete.Column("PaymentMethod").FromTable("Debts");
        Delete.Column("LastPaymentDate").FromTable("Debts");
        Delete.Column("PaymentDayOfMonth").FromTable("Debts");
    }
}
