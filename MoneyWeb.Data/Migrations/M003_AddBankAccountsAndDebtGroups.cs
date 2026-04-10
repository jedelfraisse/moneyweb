using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410003)]
public class M003_AddBankAccountsAndDebtGroups : Migration
{
    public override void Up()
    {
        Create.Table("BankAccounts")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("AccountType").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CurrentBalance").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_BankAccounts_Users")
            .FromTable("BankAccounts").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Table("DebtGroups")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("UserId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("BankAccountId").AsInt32().Nullable()
            .WithColumn("Strategy").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("MonthlyBudget").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_DebtGroups_Users")
            .FromTable("DebtGroups").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_DebtGroups_BankAccounts")
            .FromTable("DebtGroups").ForeignColumn("BankAccountId")
            .ToTable("BankAccounts").PrimaryColumn("Id");

        Alter.Table("Debts")
            .AddColumn("GroupId").AsInt32().Nullable()
            .AddColumn("GroupSortOrder").AsInt32().NotNullable().WithDefaultValue(0);

        Create.ForeignKey("FK_Debts_DebtGroups")
            .FromTable("Debts").ForeignColumn("GroupId")
            .ToTable("DebtGroups").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Debts_DebtGroups").OnTable("Debts");
        Delete.Column("GroupId").FromTable("Debts");
        Delete.Column("GroupSortOrder").FromTable("Debts");

        Delete.ForeignKey("FK_DebtGroups_BankAccounts").OnTable("DebtGroups");
        Delete.ForeignKey("FK_DebtGroups_Users").OnTable("DebtGroups");
        Delete.Table("DebtGroups");

        Delete.ForeignKey("FK_BankAccounts_Users").OnTable("BankAccounts");
        Delete.Table("BankAccounts");
    }
}
