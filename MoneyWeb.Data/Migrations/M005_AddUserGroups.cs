using FluentMigrator;

namespace MoneyWeb.Data.Migrations;

[Migration(20260410005)]
public class M005_AddUserGroups : Migration
{
    public override void Up()
    {
        Create.Table("UserGroups")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("OwnerId").AsInt32().NotNullable()
            .WithColumn("Name").AsString(256).NotNullable()
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_UserGroups_Users")
            .FromTable("UserGroups").ForeignColumn("OwnerId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Table("UserGroupMembers")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("GroupId").AsInt32().NotNullable()
            .WithColumn("UserId").AsInt32().Nullable()
            .WithColumn("InvitedEmail").AsString(256).NotNullable()
            .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CanSeeAll").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("InvitedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("JoinedAt").AsDateTime2().Nullable();

        Create.ForeignKey("FK_UserGroupMembers_Groups")
            .FromTable("UserGroupMembers").ForeignColumn("GroupId")
            .ToTable("UserGroups").PrimaryColumn("Id");

        Create.ForeignKey("FK_UserGroupMembers_Users")
            .FromTable("UserGroupMembers").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_UserGroupMembers_GroupEmail")
            .OnTable("UserGroupMembers")
            .OnColumn("GroupId").Ascending()
            .OnColumn("InvitedEmail").Ascending()
            .WithOptions().Unique();

        Alter.Table("Debts").AddColumn("SharingGroupId").AsInt32().Nullable();
        Alter.Table("Income").AddColumn("SharingGroupId").AsInt32().Nullable();
        Alter.Table("BankAccounts").AddColumn("SharingGroupId").AsInt32().Nullable();

        Create.ForeignKey("FK_Debts_SharingGroup")
            .FromTable("Debts").ForeignColumn("SharingGroupId")
            .ToTable("UserGroups").PrimaryColumn("Id");

        Create.ForeignKey("FK_Income_SharingGroup")
            .FromTable("Income").ForeignColumn("SharingGroupId")
            .ToTable("UserGroups").PrimaryColumn("Id");

        Create.ForeignKey("FK_BankAccounts_SharingGroup")
            .FromTable("BankAccounts").ForeignColumn("SharingGroupId")
            .ToTable("UserGroups").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_BankAccounts_SharingGroup").OnTable("BankAccounts");
        Delete.ForeignKey("FK_Income_SharingGroup").OnTable("Income");
        Delete.ForeignKey("FK_Debts_SharingGroup").OnTable("Debts");

        Delete.Column("SharingGroupId").FromTable("BankAccounts");
        Delete.Column("SharingGroupId").FromTable("Income");
        Delete.Column("SharingGroupId").FromTable("Debts");

        Delete.Index("IX_UserGroupMembers_GroupEmail").OnTable("UserGroupMembers");
        Delete.ForeignKey("FK_UserGroupMembers_Users").OnTable("UserGroupMembers");
        Delete.ForeignKey("FK_UserGroupMembers_Groups").OnTable("UserGroupMembers");
        Delete.Table("UserGroupMembers");

        Delete.ForeignKey("FK_UserGroups_Users").OnTable("UserGroups");
        Delete.Table("UserGroups");
    }
}
