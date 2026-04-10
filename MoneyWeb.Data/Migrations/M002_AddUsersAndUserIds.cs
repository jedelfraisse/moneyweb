using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410002)]
public class M002_AddUsersAndUserIds : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("EntraObjectId").AsString(36).NotNullable().Unique()
            .WithColumn("Email").AsString(256).NotNullable()
            .WithColumn("DisplayName").AsString(256).NotNullable()
            .WithColumn("IsApproved").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("IsAdmin").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Alter.Table("Debts").AddColumn("UserId").AsInt32().NotNullable().WithDefaultValue(0);
        Alter.Table("Bills").AddColumn("UserId").AsInt32().NotNullable().WithDefaultValue(0);
        Alter.Table("Loans").AddColumn("UserId").AsInt32().NotNullable().WithDefaultValue(0);
        Alter.Table("CashFlowTransactions").AddColumn("UserId").AsInt32().NotNullable().WithDefaultValue(0);

        Create.ForeignKey("FK_Debts_Users").FromTable("Debts").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("Id");
        Create.ForeignKey("FK_Bills_Users").FromTable("Bills").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("Id");
        Create.ForeignKey("FK_Loans_Users").FromTable("Loans").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("Id");
        Create.ForeignKey("FK_CashFlowTransactions_Users").FromTable("CashFlowTransactions").ForeignColumn("UserId").ToTable("Users").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_Debts_Users").OnTable("Debts");
        Delete.ForeignKey("FK_Bills_Users").OnTable("Bills");
        Delete.ForeignKey("FK_Loans_Users").OnTable("Loans");
        Delete.ForeignKey("FK_CashFlowTransactions_Users").OnTable("CashFlowTransactions");

        Delete.Column("UserId").FromTable("Debts");
        Delete.Column("UserId").FromTable("Bills");
        Delete.Column("UserId").FromTable("Loans");
        Delete.Column("UserId").FromTable("CashFlowTransactions");

        Delete.Table("Users");
    }
}
