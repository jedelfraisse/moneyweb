using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260411006)]
public class M013_AddDebtBankAccount : Migration
{
    public override void Up()
    {
        Alter.Table("Debts")
            .AddColumn("BankAccountId").AsInt32().Nullable();

        Create.ForeignKey("FK_Debts_BankAccounts")
            .FromTable("Debts").ForeignColumn("BankAccountId")
            .ToTable("BankAccounts").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Debts_BankAccounts").OnTable("Debts");
        Delete.Column("BankAccountId").FromTable("Debts");
    }
}
